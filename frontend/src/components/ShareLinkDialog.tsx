/**
 * ShareLinkDialog — admin share-token generation form.
 *
 * Renders the `DialogContent` for the share-link flow: a form with Name,
 * Filter, and EndDate fields managed by TanStack Form. Validation uses
 * `zod/mini` schemas passed as Standard Schema validators — name and
 * filter must be non-blank, end date must be in the future. Errors appear
 * after the first submit attempt; once visible they update live as the
 * user corrects each field.
 *
 * The owning trigger (added in a later phase) wraps this in
 * `<Dialog.Dialog>` and controls open state; on a valid submit
 * `onGenerate` fires with the trimmed params.
 */
import { type ChangeEvent, type SyntheticEvent, useCallback, useState } from "react";
import { refine, string } from "zod/mini";
import Button from "@components/ui/Button";
import Dialog from "@components/ui/Dialog";
import Input from "@components/ui/Input";
import Label from "@components/ui/Label";
import { useForm } from "@tanstack/react-form";

interface ShareFormValues {
  name: string;
  filter: string;
  endDate: string;
}

interface ShareLinkDialogProps {
  /** Fired with validated, trimmed params when the form is submitted. */
  onGenerate: (params: ShareFormValues) => void;
}

const nameSchema = string().check(refine((value) => value.trim() !== "", { error: "A name must be specified." }));
const filterSchema = string().check(refine((value) => value.trim() !== "", { error: "A filter must be specified." }));
const endDateSchema = string().check(
  refine((value) => value !== "", { error: "An end date must be specified." }),
  refine((value) => !Number.isNaN(new Date(value).getTime()), { error: "Invalid date." }),
  refine((value) => new Date(value).getTime() > Date.now(), { error: "The end date must be in the future." }),
);

const nameValidators = { onChange: nameSchema, onSubmit: nameSchema };
const filterValidators = { onChange: filterSchema, onSubmit: filterSchema };
const endDateValidators = { onChange: endDateSchema, onSubmit: endDateSchema };

interface ShareFieldProps {
  id: string;
  label: string;
  type?: string;
  value: string;
  error?: string;
  onChange: (value: string) => void;
  onBlur: () => void;
}

function ShareField({ id, label, type = "text", value, error, onChange, onBlur }: ShareFieldProps): React.JSX.Element {
  const handleInputChange = useCallback(
    (event: ChangeEvent<HTMLInputElement>) => onChange(event.target.value),
    [onChange],
  );
  return (
    <div className="flex flex-col gap-1">
      <Label htmlFor={id}>{label}</Label>
      <Input id={id} type={type} value={value} onChange={handleInputChange} onBlur={onBlur} aria-invalid={error !== undefined} />
      {error !== undefined && <span className="text-xs text-destructive">{error}</span>}
    </div>
  );
}

function ShareDialogHeader(): React.JSX.Element {
  return (
    <Dialog.DialogHeader>
      <Dialog.DialogTitle>Generate share link</Dialog.DialogTitle>
      <Dialog.DialogDescription>
        Create a time-limited link that grants access to folders matching the filter.
      </Dialog.DialogDescription>
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

function useShareForm(onGenerate: (params: ShareFormValues) => void) {
  const [attemptedSubmit, setAttemptedSubmit] = useState(false);
  const form = useForm({
    defaultValues: { name: "", filter: "", endDate: "" } as ShareFormValues,
    onSubmit: ({ value }) => {
      onGenerate({ name: value.name.trim(), filter: value.filter.trim(), endDate: value.endDate });
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

function ShareFormFields({ form, showErrors }: ShareFormFieldsProps): React.JSX.Element {
  return (
    <div className="flex flex-col gap-3">
      <form.Field name="name" validators={nameValidators}>
        {(field) => (
          <ShareField
            id={field.name}
            label="Name"
            value={field.state.value}
            error={showErrors ? fieldErrorMessage(field.state.meta.errors) : undefined}
            onChange={field.handleChange}
            onBlur={field.handleBlur}
          />
        )}
      </form.Field>
      <form.Field name="filter" validators={filterValidators}>
        {(field) => (
          <ShareField
            id={field.name}
            label="Filter"
            value={field.state.value}
            error={showErrors ? fieldErrorMessage(field.state.meta.errors) : undefined}
            onChange={field.handleChange}
            onBlur={field.handleBlur}
          />
        )}
      </form.Field>
      <form.Field name="endDate" validators={endDateValidators}>
        {(field) => (
          <ShareField
            id={field.name}
            label="End date"
            type="datetime-local"
            value={field.state.value}
            error={showErrors ? fieldErrorMessage(field.state.meta.errors) : undefined}
            onChange={field.handleChange}
            onBlur={field.handleBlur}
          />
        )}
      </form.Field>
    </div>
  );
}

function ShareDialogFooter(): React.JSX.Element {
  return (
    <Dialog.DialogFooter>
      <Button type="submit">Generate</Button>
    </Dialog.DialogFooter>
  );
}

export default function ShareLinkDialog({ onGenerate }: ShareLinkDialogProps): React.JSX.Element {
  const { form, showErrors, handleSubmit } = useShareForm(onGenerate);
  return (
    <Dialog.DialogContent>
      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        <ShareDialogHeader />
        <ShareFormFields form={form} showErrors={showErrors} />
        <ShareDialogFooter />
      </form>
    </Dialog.DialogContent>
  );
}
