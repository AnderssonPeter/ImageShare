import { type FolderEntry, type generateToken, type getContent } from "@lib/api/generated";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { type ReactNode, useCallback } from "react";
import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { ApiError } from "@lib/api/errors";
import Button from "@components/ui/Button";
import ShareDialogProvider from "@components/ShareDialogProvider";
import { useShareDialog } from "@lib/shareDialogContext";

const { mockGenerateToken, mockGetContent, mockToastSuccess } = vi.hoisted(() => ({
  mockGenerateToken: vi.fn<typeof generateToken>(),
  mockGetContent: vi.fn<typeof getContent>(),
  mockToastSuccess: vi.fn<(message: string) => void>(),
}));

vi.mock(import("@lib/api/generated/sdk.gen"), async (importOriginal) => {
  const actual = (await importOriginal()) as Record<string, unknown>;
  return {
    ...actual,
    generateToken: mockGenerateToken as never,
    getContent: mockGetContent as never,
  };
});

vi.mock(import("sonner"), async (importOriginal) => {
  const actual = (await importOriginal()) as Record<string, unknown>;
  const toast = actual.toast as Record<string, unknown>;
  return { ...actual, toast: { ...toast, success: mockToastSuccess } as never };
});

const TOKEN = "eyJhbGciOiJIUzI1NiJ9.payload.signature";
const FUTURE_DATE = "2099-12-31T23:59";

function emptyPageResponse() {
  return {
    data: { items: [] as FolderEntry[], page: 1, pageSize: 500, totalCount: 0 },
    request: new Request("http://localhost/api/content"),
    response: new Response(),
  } as never;
}

function tokenResponse(token: string) {
  return {
    data: token,
    request: new Request("http://localhost/api/authentication/token/generate"),
    response: new Response(),
  } as never;
}

/** Mock: content listing succeeds, token endpoint returns the JWT. */
function stubTokenSuccess(): void {
  mockGetContent.mockResolvedValue(emptyPageResponse());
  mockGenerateToken.mockResolvedValue(tokenResponse(TOKEN));
}

/** Mock: content listing succeeds, token endpoint rejects with an ApiError. */
function stubTokenError(error: ApiError): void {
  mockGetContent.mockResolvedValue(emptyPageResponse());
  mockGenerateToken.mockRejectedValue(error);
}

/** Mock: content listing succeeds, token endpoint never resolves (stays pending). */
function stubTokenPending(): void {
  mockGetContent.mockResolvedValue(emptyPageResponse());
  mockGenerateToken.mockReturnValue(Promise.race<never>([]) as never);
}

/** A consumer that opens the share dialog, optionally with a return URL. */
function ShareTrigger({ initialReturnUrl }: { initialReturnUrl?: string }): React.JSX.Element {
  const { openShare } = useShareDialog();
  const handleOpen = useCallback(() => openShare(initialReturnUrl), [openShare, initialReturnUrl]);
  return <Button onClick={handleOpen}>Open share</Button>;
}

/**
 * A consumer wired like the app-bar trigger — forwards the click event straight
 * to `openShare` (which must ignore it), like `onShare={openShare}` would.
 */
function EventShareTrigger(): React.JSX.Element {
  const { openShare } = useShareDialog();
  return <Button onClick={openShare as (event: unknown) => void}>Open via event</Button>;
}

function renderWith(trigger: ReactNode, enabled = true): QueryClient {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  function Wrapper({ children }: { children: ReactNode }) {
    return (
      <QueryClientProvider client={queryClient}>
        <ShareDialogProvider enabled={enabled}>{children}</ShareDialogProvider>
      </QueryClientProvider>
    );
  }
  render(<Wrapper>{trigger}</Wrapper>);
  return queryClient;
}

function renderProvider(enabled = true, initialReturnUrl?: string): QueryClient {
  return renderWith(<ShareTrigger initialReturnUrl={initialReturnUrl} />, enabled);
}

function renderProviderWithEventTrigger(): QueryClient {
  return renderWith(<EventShareTrigger />);
}

function fillAndSubmitForm(returnUrl?: string): void {
  fireEvent.change(screen.getByLabelText("Name"), { target: { value: "Test User" } });
  fireEvent.click(screen.getByLabelText("All folders"));
  fireEvent.change(screen.getByLabelText("End date"), { target: { value: FUTURE_DATE } });
  if (returnUrl !== undefined) {
    fireEvent.change(screen.getByLabelText("Return URL"), { target: { value: returnUrl } });
  }
  fireEvent.click(screen.getByRole("button", { name: "Generate" }));
}

function assertTokenPath(): void {
  const [call] = mockGenerateToken.mock.calls;
  expect(call).toBeDefined();
  const path = call?.[0]?.path;
  expect(path).toMatchObject({ name: "Test User", filter: "*" });
  expect(path?.endDate).toBe(FUTURE_DATE);
}

describe("shareDialogProvider is inert when disabled", () => {
  it("does not render the form dialog for non-admin users", () => {
    expect.assertions(1);
    // Arrange + Act
    stubTokenSuccess();
    renderProvider(false);
    // Assert — the trigger exists but no share dialog content is mounted
    expect(screen.queryByLabelText("Name")).not.toBeInTheDocument();
  }, 1000);
});

describe("shareDialogProvider event-leak guard", () => {
  it("leaves the Return URL empty when openShare receives a click event", async () => {
    expect.hasAssertions();
    // Arrange — wire `openShare` straight to onClick (as the app bar does), so
    // React forwards the MouseEvent as the first argument.
    stubTokenSuccess();
    renderProviderWithEventTrigger();
    // Act
    fireEvent.click(screen.getByRole("button", { name: "Open via event" }));
    // Assert — the Return URL field is empty, not "[object Object]"
    await waitFor(() => {
      expect(screen.getByLabelText("Return URL")).toBeInTheDocument();
    });
    expect(screen.getByLabelText("Return URL")).toHaveValue("");
  }, 2000);
});

describe("shareDialogProvider token generation", () => {
  it("calls the token endpoint and opens the result dialog", async () => {
    expect.hasAssertions();
    // Arrange
    stubTokenSuccess();
    mockToastSuccess.mockReset();
    renderProvider();
    // Act — open the form and submit a valid form
    fireEvent.click(screen.getByRole("button", { name: "Open share" }));
    fillAndSubmitForm();
    // Assert — the result dialog opens (QR code appears) and the SDK was hit
    await waitFor(() => {
      expect(screen.getByText("Share link generated")).toBeInTheDocument();
    });
    assertTokenPath();
    expect(mockToastSuccess).toHaveBeenCalledWith("Share link generated");
  }, 2000);

  it("prefills the Return URL field when opened with one", async () => {
    expect.hasAssertions();
    // Arrange
    stubTokenSuccess();
    renderProvider(true, "/browse/photos?image=cat.jpg");
    // Act
    fireEvent.click(screen.getByRole("button", { name: "Open share" }));
    // Assert
    await waitFor(() => {
      expect(screen.getByLabelText("Return URL")).toHaveValue("/browse/photos?image=cat.jpg");
    });
  }, 2000);
});

describe("shareDialogProvider error handling", () => {
  it("surfaces RFC 7807 error messages in the still-open form dialog", async () => {
    expect.hasAssertions();
    // Arrange — 400 (and likewise 403) surface their detail in the form, not the result
    stubTokenError(new ApiError(400, { detail: "A filter must be specified." }));
    renderProvider();
    // Act
    fireEvent.click(screen.getByRole("button", { name: "Open share" }));
    fillAndSubmitForm();
    // Assert
    await waitFor(() =>
      expect(screen.getByText("A filter must be specified.")).toBeInTheDocument(),
    );
    expect(screen.queryByText("Share link generated")).not.toBeInTheDocument();
  }, 2000);

  it("shows a loading state on the Generate button while submitting", async () => {
    expect.hasAssertions();
    // Arrange — never-resolving token call keeps the mutation pending
    stubTokenPending();
    renderProvider();
    fireEvent.click(screen.getByRole("button", { name: "Open share" }));
    // Act
    fillAndSubmitForm();
    // Assert — the button switches to its loading label
    await waitFor(() => expect(screen.getByRole("button", { name: "Generating…" })).toBeDisabled());
  }, 2000);
});

describe("shareDialogProvider return URL", () => {
  it("appends the trimmed return URL to the share link", async () => {
    expect.hasAssertions();
    // Arrange
    stubTokenSuccess();
    renderProvider();
    fireEvent.click(screen.getByRole("button", { name: "Open share" }));
    // Act — a full URL is entered; only the path should reach the share link
    fillAndSubmitForm("https://imageshare.example/browse/photos?image=cat.jpg");
    await waitFor(() => expect(screen.getByText("Share link generated")).toBeInTheDocument());
    // Assert — the Copy action copies the share URL with the trimmed returnUrl
    const writeText = vi.fn<() => Promise<void>>();
    Object.defineProperty(globalThis.navigator, "clipboard", {
      value: { writeText },
      configurable: true,
    });
    fireEvent.click(screen.getByRole("button", { name: "Copy link" }));
    await waitFor(() =>
      expect(writeText).toHaveBeenCalledWith(
        expect.stringContaining(`?returnUrl=${encodeURIComponent("/browse/photos?image=cat.jpg")}`),
      ),
    );
  }, 2000);
});

/** Open the form fresh and assert all fields are empty. */
async function expectFreshForm(): Promise<void> {
  fireEvent.click(screen.getByRole("button", { name: "Open share" }));
  await waitFor(() => {
    expect(screen.getByLabelText("Name")).toHaveValue("");
    expect(screen.getByLabelText("End date")).toHaveValue("");
    expect(screen.getByLabelText("Return URL")).toHaveValue("");
  });
}

/** Close the open dialog and wait for `disappearingText` to unmount. */
async function closeDialog(disappearingText: string): Promise<void> {
  fireEvent.click(screen.getByRole("button", { name: "Close" }));
  await waitFor(() => expect(screen.queryByText(disappearingText)).not.toBeInTheDocument());
}

describe("shareDialogProvider form reset", () => {
  it("clears the form fields after generating a link and reopening", async () => {
    expect.hasAssertions();
    // Arrange
    stubTokenSuccess();
    renderProvider();
    // Act — first session: fill and generate, then close the result
    fireEvent.click(screen.getByRole("button", { name: "Open share" }));
    await waitFor(() => expect(screen.getByLabelText("All folders")).toBeInTheDocument());
    fillAndSubmitForm();
    await waitFor(() => expect(screen.getByText("Share link generated")).toBeInTheDocument());
    await closeDialog("Share link generated");
    // Assert — reopening shows a fresh form
    await expectFreshForm();
  }, 3000);

  it("clears the form fields after closing without submitting and reopening", async () => {
    expect.hasAssertions();
    // Arrange
    stubTokenSuccess();
    renderProvider();
    // Act — open, fill some fields, then close via the dialog close button
    fireEvent.click(screen.getByRole("button", { name: "Open share" }));
    await waitFor(() => expect(screen.getByLabelText("All folders")).toBeInTheDocument());
    fireEvent.change(screen.getByLabelText("Name"), { target: { value: "Tester" } });
    fireEvent.click(screen.getByLabelText("All folders"));
    await closeDialog("Generate share link");
    // Assert — reopening shows empty fields and an unchecked "All folders"
    fireEvent.click(screen.getByRole("button", { name: "Open share" }));
    await waitFor(() => {
      expect(screen.getByLabelText("Name")).toHaveValue("");
      expect(screen.queryByLabelText("All folders")).not.toBeChecked();
    });
  }, 3000);
});
