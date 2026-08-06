import { type ChangeEvent, useCallback } from "react";
import Input from "@components/ui/Input";
import Label from "@components/ui/Label";

interface ShareFieldProps {
  id: string;
  label: string;
  type?: string;
  value: string;
  error?: string;
  onChange: (value: string) => void;
  onBlur: () => void;
}

export default function ShareField({
  id,
  label,
  type = "text",
  value,
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
      <Label htmlFor={id}>{label}</Label>
      <Input
        id={id}
        type={type}
        value={value}
        onChange={handleInputChange}
        onBlur={onBlur}
        aria-invalid={error !== undefined}
      />
      {error !== undefined && <span className="text-xs text-destructive">{error}</span>}
    </div>
  );
}
