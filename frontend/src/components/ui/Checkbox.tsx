import { type ChangeEvent, type ReactNode, useCallback } from "react";
import cn from "@lib/utils";

interface CheckboxProps {
  checked: boolean;
  onChange: (checked: boolean) => void;
  label: ReactNode;
  className?: string;
  children?: ReactNode;
}

function Checkbox({
  checked,
  onChange,
  label,
  className,
  children,
}: CheckboxProps): React.JSX.Element {
  const handleChange = useCallback(
    (event: ChangeEvent<HTMLInputElement>) => onChange(event.target.checked),
    [onChange],
  );
  return (
    <label className={cn("flex items-center gap-2 text-sm", className)}>
      <input type="checkbox" checked={checked} onChange={handleChange} />
      {children}
      {label}
    </label>
  );
}

export default Checkbox;
