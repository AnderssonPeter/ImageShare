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
import { type Translate, useTranslation } from "@lib/i18n";

interface DownloadFormValues {
  recursive: boolean;
  format: string;
}

interface FormatOption {
  value: string;
  label: string;
  description?: string;
}

const FORMAT_KEYS = [
  {
    value: "avif",
    labelKey: "download.formats.avif",
    descriptionKey: "download.formatDescriptions.avif",
  },
  {
    value: "webp",
    labelKey: "download.formats.webp",
    descriptionKey: "download.formatDescriptions.webp",
  },
  {
    value: "jpg",
    labelKey: "download.formats.jpg",
    descriptionKey: "download.formatDescriptions.jpg",
  },
  {
    value: "",
    labelKey: "download.formats.all",
    descriptionKey: "download.formatDescriptions.all",
  },
] as const;

function buildFormatOptions(translate: Translate): readonly FormatOption[] {
  return FORMAT_KEYS.map((option) => ({
    value: option.value,
    label: translate(option.labelKey),
    description: translate(option.descriptionKey),
  }));
}

interface DownloadDialogProps {
  /** Fired with form values when the user confirms the download. */
  onDownload: (values: DownloadFormValues) => void;
}

function DownloadDialogHeader(): React.JSX.Element {
  const { t: translate } = useTranslation();
  return (
    <Dialog.DialogHeader>
      <Dialog.DialogTitle>{translate("download.title")}</Dialog.DialogTitle>
      <Dialog.DialogDescription>{translate("download.description")}</Dialog.DialogDescription>
    </Dialog.DialogHeader>
  );
}

type DownloadForm = ReturnType<typeof useDownloadForm>["form"];

interface DownloadDialogBodyProps {
  form: DownloadForm;
}

function DownloadDialogBody({ form }: DownloadDialogBodyProps): React.JSX.Element {
  const { t: translate } = useTranslation();
  const formatOptions = buildFormatOptions(translate);
  return (
    <div className="flex flex-col gap-3">
      <form.Field name="recursive">
        {(field) => (
          <Checkbox
            checked={field.state.value}
            onChange={field.handleChange}
            label={translate("download.recursive")}
          />
        )}
      </form.Field>
      <form.Field name="format">
        {(field) => (
          <RadioGroup
            value={field.state.value}
            onChange={field.handleChange}
            options={formatOptions}
            name="format"
            legend={translate("download.formatLegend")}
          />
        )}
      </form.Field>
    </div>
  );
}

function DownloadDialogFooter(): React.JSX.Element {
  const { t: translate } = useTranslation();
  return (
    <Dialog.DialogFooter>
      <Button type="submit">{translate("download.button")}</Button>
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
