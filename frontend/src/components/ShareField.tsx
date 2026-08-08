import { type ChangeEvent, useCallback } from "react";
import Input from "@components/ui/Input";
import Label from "@components/ui/Label";

interface ShareFieldProps {
  id: string;
  value: string;
  onChange: (value: string) => void;
  onBlur: () => void;
  /** Visible label; omitted for label-less fields (e.g. the Return URL). */
  label?: string;
  type?: string;
  placeholder?: string;
  /** Accessible name when no visible label is rendered. */
  ariaLabel?: string;
  inputMode?: "none" | "text" | "tel" | "url" | "email" | "numeric" | "decimal" | "search";
  error?: string;
}

export default function ShareField({
  id,
  label,
  type = "text",
  value,
  placeholder,
  ariaLabel,
  inputMode,
  error,
  onChange,
  onBlur,
}: ShareFieldProps): React.JSX.Element {
  const handleInputChange = useCallback(
    (event: ChangeEvent<HTMLInputElement>) => onChange(event.target.value),
    [onChange],
  );
  return (
    <div className="flex flex-col gap-1">
      {label !== undefined && <Label htmlFor={id}>{label}</Label>}
      <Input
        id={id}
        type={type}
        value={value}
        placeholder={placeholder}
        onChange={handleInputChange}
        onBlur={onBlur}
        inputMode={inputMode}
        aria-label={ariaLabel}
        aria-invalid={error !== undefined}
      />
      {error !== undefined && <span className="text-xs text-destructive">{error}</span>}
    </div>
  );
}
