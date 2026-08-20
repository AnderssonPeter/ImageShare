import { ApiError, type ProblemDetails } from "@lib/api/errors";
import { buildLoginUrl, resolveErrorAction, type ErrorContext } from "@lib/api/queryErrorHandler";
import { translate } from "@lib/i18n";
import { describe, expect, it } from "vitest";

const translateFn = translate();

function context(currentPath: string, authSource?: ErrorContext["authSource"]): ErrorContext {
  return { currentPath, translate: translateFn, authSource };
}

describe("buildLoginUrl builder", () => {
  it("encodes the current path as returnUrl", () => {
    expect.assertions(1);
    const path = "/browse/sub%20folder?x=1#frag";
    expect(buildLoginUrl(path)).toBe(
      `/api/authentication/login?returnUrl=${encodeURIComponent(path)}`,
    );
  }, 1000);

  it("uses '/' when the current path is empty", () => {
    expect.assertions(1);
    expect(buildLoginUrl("")).toBe("/api/authentication/login?returnUrl=%2F");
  }, 1000);
});

describe("resolveErrorAction routing", () => {
  it("redirects to the login endpoint on 401 with the current path", () => {
    expect.assertions(1);
    const error = new ApiError(401, undefined);
    expect(resolveErrorAction(error, context("/browse/animals"))).toStrictEqual({
      kind: "redirect",
      url: `/api/authentication/login?returnUrl=${encodeURIComponent("/browse/animals")}`,
    });
  }, 1000);

  it("ignores 404 so the owning component can render an empty state", () => {
    expect.assertions(1);
    const error = new ApiError(404, undefined);
    expect(resolveErrorAction(error, context("/browse/missing"))).toStrictEqual({
      kind: "ignore",
    });
  }, 1000);
});

describe("resolveErrorAction 401 by auth source", () => {
  it("toasts session-expired with a sign-in action for OpenIdConnect", () => {
    expect.assertions(1);
    const error = new ApiError(401, undefined);
    expect(resolveErrorAction(error, context("/browse/animals", "OpenIdConnect"))).toStrictEqual({
      kind: "toast",
      message: "Your session has expired.",
      action: {
        label: "Sign in",
        url: `/api/authentication/login?returnUrl=${encodeURIComponent("/browse/animals")}`,
      },
    });
  }, 1000);

  it("toasts a no-access message without an action for Jwt", () => {
    expect.assertions(1);
    const error = new ApiError(401, undefined);
    expect(resolveErrorAction(error, context("/browse", "Jwt"))).toStrictEqual({
      kind: "toast",
      message: "You no longer have access.",
    });
  }, 1000);

  it("ignores a 401 for ApiKey (cannot expire mid-session)", () => {
    expect.assertions(1);
    const error = new ApiError(401, undefined);
    expect(resolveErrorAction(error, context("/browse", "ApiKey"))).toStrictEqual({
      kind: "ignore",
    });
  }, 1000);

  it("falls back to a redirect when the source is unknown", () => {
    expect.assertions(1);
    const error = new ApiError(401, undefined);
    expect(resolveErrorAction(error, context("/browse"))).toStrictEqual({
      kind: "redirect",
      url: `/api/authentication/login?returnUrl=${encodeURIComponent("/browse")}`,
    });
  }, 1000);
});

describe("resolveErrorAction toasts", () => {
  it("toasts the RFC 7807 detail on 403", () => {
    expect.assertions(1);
    const problem: ProblemDetails = { title: "Forbidden", detail: "Not allowed." };
    const error = new ApiError(403, problem);
    expect(resolveErrorAction(error, context("/admin"))).toStrictEqual({
      kind: "toast",
      message: "Not allowed.",
    });
  }, 1000);

  it("toasts the RFC 7807 title when detail is absent on 406", () => {
    expect.assertions(1);
    const problem: ProblemDetails = { title: "Not Acceptable" };
    const error = new ApiError(406, problem);
    expect(resolveErrorAction(error, context("/browse"))).toStrictEqual({
      kind: "toast",
      message: "Not Acceptable",
    });
  }, 1000);

  it("toasts a generic message for unexpected HTTP statuses", () => {
    expect.assertions(1);
    const error = new ApiError(500, { detail: "Server blew up." });
    expect(resolveErrorAction(error, context("/browse"))).toStrictEqual({
      kind: "toast",
      message: "Server blew up.",
    });
  }, 1000);

  it("falls back to the error message for non-ApiError errors", () => {
    expect.assertions(1);
    expect(resolveErrorAction(new TypeError("network gone"), context("/browse"))).toStrictEqual({
      kind: "toast",
      message: "network gone",
    });
  }, 1000);

  it("falls back to a generic string for non-Error primitives", () => {
    expect.assertions(1);
    expect(resolveErrorAction("oops", context("/browse"))).toStrictEqual({
      kind: "toast",
      message: "Something went wrong.",
    });
  }, 1000);
});
