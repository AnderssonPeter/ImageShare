import { type ChangeEvent, useCallback } from "react";
import cn from "@lib/utils";

interface CheckboxProps {
  checked: boolean;
  onChange: (checked: boolean) => void;
  label: string;
  className?: string;
}

function Checkbox({ checked, onChange, label, className }: CheckboxProps): React.JSX.Element {
  const handleChange = useCallback(
    (event: ChangeEvent<HTMLInputElement>) => onChange(event.target.checked),
    [onChange],
  );
  return (
    <label className={cn("flex items-center gap-2 text-sm", className)}>
      <input type="checkbox" checked={checked} onChange={handleChange} />
      {label}
    </label>
  );
}

export default Checkbox;