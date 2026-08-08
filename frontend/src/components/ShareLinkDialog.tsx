import { type SyntheticEvent, useCallback, useState } from "react";
import { refine, string } from "zod/mini";
import Button from "@components/ui/Button";
import Dialog from "@components/ui/Dialog";
import FilterBuilder from "@components/FilterBuilder";
import ShareField from "@components/ShareField";
import { trimReturnUrl } from "@lib/api/urls";
import { useForm } from "@tanstack/react-form";
import { useRootFolders } from "@lib/api/contentQueries";
import { type Translate, useTranslation } from "@lib/i18n";

interface ShareFormValues {
  name: string;
  filter: string;
  endDate: string;
  returnUrl: string;
}

interface ShareLinkDialogProps {
  /** Fired with validated, trimmed params when the form is submitted. */
  onGenerate: (params: ShareFormValues) => void;
  /** RFC 7807 error message from the token endpoint, if the last submit failed. */
  submitError?: string;
  /** Whether the token request is in flight; disables the submit button. */
  isSubmitting?: boolean;
  /** Optional value used to prefill the Return URL field when the dialog opens. */
  initialReturnUrl?: string;
}

function nameSchema(translate: Translate) {
  return string().check(
    refine((value) => value.trim() !== "", { error: translate("share.errors.nameRequired") }),
  );
}

function filterSchema(translate: Translate) {
  return string().check(
    refine((value) => value.trim() !== "", { error: translate("share.errors.filterRequired") }),
  );
}

function endDateSchema(translate: Translate) {
  return string()
    .check(refine((value) => value !== "", { error: translate("share.errors.endDateRequired") }))
    .check(
      refine((value) => !Number.isNaN(new Date(value).getTime()), {
        error: translate("share.errors.dateInvalid"),
      }),
    )
    .check(
      refine((value) => new Date(value).getTime() > Date.now(), {
        error: translate("share.errors.endDateFuture"),
      }),
    );
}

function validatorsFor(name: "name" | "endDate", translate: Translate) {
  const schema = name === "name" ? nameSchema(translate) : endDateSchema(translate);
  return { onChange: schema, onSubmit: schema };
}

function filterValidators(translate: Translate) {
  const schema = filterSchema(translate);
  return { onChange: schema, onSubmit: schema };
}

const EMPTY_FOLDERS: string[] = [];

function ShareDialogHeader(): React.JSX.Element {
  const { t: translate } = useTranslation();
  return (
    <Dialog.DialogHeader>
      <Dialog.DialogTitle>{translate("share.generateTitle")}</Dialog.DialogTitle>
      <Dialog.DialogDescription>{translate("share.generateDescription")}</Dialog.DialogDescription>
    </Dialog.DialogHeader>
  );
}

function fieldErrorMessage(errors: unknown[]): string | undefined {
  if (errors.length === 0) {
    return undefined;
  }
  const [error] = errors;
  if (typeof error === "string") {
    return error;
  }
  if (error !== null && typeof error === "object" && "message" in error) {
    return String((error as { message: unknown }).message);
  }
  return undefined;
}

interface UseShareFormOptions {
  onGenerate: (params: ShareFormValues) => void;
  initialReturnUrl?: string;
}

function useShareForm({ onGenerate, initialReturnUrl }: UseShareFormOptions) {
  const [attemptedSubmit, setAttemptedSubmit] = useState(false);
  const form = useForm({
    defaultValues: {
      name: "",
      filter: "",
      endDate: "",
      returnUrl: initialReturnUrl ?? "",
    } as ShareFormValues,
    onSubmit: ({ value }) => {
      onGenerate({
        name: value.name.trim(),
        filter: value.filter.trim(),
        endDate: value.endDate,
        returnUrl: trimReturnUrl(value.returnUrl),
      });
    },
  });
  const handleSubmit = useCallback(
    (event: SyntheticEvent) => {
      event.preventDefault();
      setAttemptedSubmit(true);
      void form.handleSubmit();
    },
    [form],
  );
  return { form, showErrors: attemptedSubmit, handleSubmit };
}

type ShareForm = ReturnType<typeof useShareForm>["form"];

interface ShareFormFieldsProps {
  form: ShareForm;
  showErrors: boolean;
}

interface TextFieldOptions {
  name: "name" | "endDate";
  label: string;
  type?: string;
  showErrors: boolean;
}

function textField(
  form: ShareForm,
  translate: Translate,
  options: TextFieldOptions,
): React.JSX.Element {
  const { name, label, type, showErrors } = options;
  return (
    <form.Field name={name} validators={validatorsFor(name, translate)}>
      {(field) => (
        <ShareField
          id={field.name}
          label={label}
          type={type}
          value={field.state.value}
          error={showErrors ? fieldErrorMessage(field.state.meta.errors) : undefined}
          onChange={field.handleChange}
          onBlur={field.handleBlur}
        />
      )}
    </form.Field>
  );
}

function ShareFormFields({ form, showErrors }: ShareFormFieldsProps): React.JSX.Element {
  const { t: translate } = useTranslation();
  const { data: folders } = useRootFolders();
  const folderNames = folders ?? EMPTY_FOLDERS;
  return (
    <div className="flex flex-col gap-3">
      {textField(form, translate, {
        name: "name",
        label: translate("share.fields.name"),
        showErrors,
      })}
      <form.Field name="filter" validators={filterValidators(translate)}>
        {(field) => (
          <FilterBuilder
            folders={folderNames}
            value={field.state.value}
            error={showErrors ? fieldErrorMessage(field.state.meta.errors) : undefined}
            onChange={field.handleChange}
          />
        )}
      </form.Field>
      {textField(form, translate, {
        name: "endDate",
        label: translate("share.fields.endDate"),
        type: "datetime-local",
        showErrors,
      })}
      <form.Field name="returnUrl">
        {(field) => (
          <ShareField
            id={field.name}
            label={translate("share.fields.returnUrl")}
            value={field.state.value}
            placeholder={translate("share.fields.returnUrlPlaceholder")}
            inputMode="url"
            onChange={field.handleChange}
            onBlur={field.handleBlur}
          />
        )}
      </form.Field>
    </div>
  );
}

function ShareSubmitError({ message }: { message: string }): React.JSX.Element {
  return (
    <p role="alert" className="text-xs text-destructive">
      {message}
    </p>
  );
}

interface ShareDialogFooterProps {
  isSubmitting: boolean;
}

function ShareDialogFooter({ isSubmitting }: ShareDialogFooterProps): React.JSX.Element {
  const { t: translate } = useTranslation();
  return (
    <Dialog.DialogFooter>
      <Button type="submit" disabled={isSubmitting}>
        {isSubmitting ? translate("share.generating") : translate("share.submit")}
      </Button>
    </Dialog.DialogFooter>
  );
}

export default function ShareLinkDialog({
  onGenerate,
  submitError,
  isSubmitting,
  initialReturnUrl,
}: ShareLinkDialogProps): React.JSX.Element {
  const { form, showErrors, handleSubmit } = useShareForm({ onGenerate, initialReturnUrl });
  return (
    <Dialog.DialogContent>
      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        <ShareDialogHeader />
        <ShareFormFields form={form} showErrors={showErrors} />
        {submitError !== undefined && <ShareSubmitError message={submitError} />}
        <ShareDialogFooter isSubmitting={isSubmitting ?? false} />
      </form>
    </Dialog.DialogContent>
  );
}
