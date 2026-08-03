/**
 * DownloadDialog — folder download dialog contents.
 *
 * Renders the `DialogContent` for the folder download flow: a recursive
 * toggle, a format radio group (avif/webp/jpg/all), and a Download action.
 * Form state (recursive, format) is managed by TanStack Form — the owning
 * trigger (`DownloadButton`) just receives the values on submit.
 *
 * The backend currently always downloads recursively (the `recursive`
 * toggle is a forward-looking UI option; it has no effect until the
 * download endpoint gains a `recursive` query parameter).
 */
import { type SyntheticEvent, useCallback } from "react";
import Button from "@components/ui/Button";
import Checkbox from "@components/ui/Checkbox";
import Dialog from "@components/ui/Dialog";
import RadioGroup from "@components/ui/RadioGroup";
import { useForm } from "@tanstack/react-form";

interface DownloadFormValues {
  recursive: boolean;
  format: string;
}

interface RadioOption {
  value: string;
  label: string;
}

interface DownloadDialogProps {
  /** Fired with form values when the user confirms the download. */
  onDownload: (values: DownloadFormValues) => void;
}

const FORMAT_OPTIONS: readonly RadioOption[] = [
  { value: "avif", label: "AVIF" },
  { value: "webp", label: "WEBP" },
  { value: "jpg", label: "JPG" },
  { value: "", label: "All formats" },
];

function DownloadDialogHeader(): React.JSX.Element {
  return (
    <Dialog.DialogHeader>
      <Dialog.DialogTitle>Download folder</Dialog.DialogTitle>
      <Dialog.DialogDescription>
        Choose the image format and whether to include subfolders.
      </Dialog.DialogDescription>
    </Dialog.DialogHeader>
  );
}

type DownloadForm = ReturnType<typeof useDownloadForm>["form"];

interface DownloadDialogBodyProps {
  form: DownloadForm;
}

function DownloadDialogBody({ form }: DownloadDialogBodyProps): React.JSX.Element {
  return (
    <div className="flex flex-col gap-3">
      <form.Field name="recursive">
        {(field) => (
          <Checkbox
            checked={field.state.value}
            onChange={field.handleChange}
            label="Recursive (include subfolders)"
          />
        )}
      </form.Field>
      <form.Field name="format">
        {(field) => (
          <RadioGroup
            value={field.state.value}
            onChange={field.handleChange}
            options={FORMAT_OPTIONS}
            name="format"
            legend="Format"
          />
        )}
      </form.Field>
    </div>
  );
}

function DownloadDialogFooter(): React.JSX.Element {
  return (
    <Dialog.DialogFooter>
      <Button type="submit">Download</Button>
    </Dialog.DialogFooter>
  );
}

function useDownloadForm(onDownload: (values: DownloadFormValues) => void) {
  const form = useForm({
    defaultValues: { recursive: true, format: "" } as DownloadFormValues,
    onSubmit: ({ value }) => {
      onDownload(value);
    },
  });
  const handleSubmit = useCallback(
    (event: SyntheticEvent) => {
      event.preventDefault();
      void form.handleSubmit();
    },
    [form],
  );
  return { form, handleSubmit };
}

export default function DownloadDialog({ onDownload }: DownloadDialogProps): React.JSX.Element {
  const { form, handleSubmit } = useDownloadForm(onDownload);
  return (
    <Dialog.DialogContent>
      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        <DownloadDialogHeader />
        <DownloadDialogBody form={form} />
        <DownloadDialogFooter />
      </form>
    </Dialog.DialogContent>
  );
}
