import { type ChangeEvent, useCallback } from "react";
import cn from "@lib/utils";

interface RadioOption {
  value: string;
  label: string;
}

interface RadioGroupProps {
  value: string;
  onChange: (value: string) => void;
  options: readonly RadioOption[];
  name: string;
  legend: string;
  className?: string;
}

function RadioGroup({
  value,
  onChange,
  options,
  name,
  legend,
  className,
}: RadioGroupProps): React.JSX.Element {
  const handleChange = useCallback(
    (event: ChangeEvent<HTMLInputElement>) => onChange(event.target.value),
    [onChange],
  );
  return (
    <fieldset className={cn("flex flex-col gap-1", className)}>
      <legend className="mb-1 text-sm text-muted-foreground">{legend}</legend>
      {options.map((option) => (
        <label key={option.value} className="flex items-center gap-2 text-sm">
          <input
            type="radio"
            name={name}
            value={option.value}
            checked={value === option.value}
            onChange={handleChange}
          />
          {option.label}
        </label>
      ))}
    </fieldset>
  );
}

export default RadioGroup;
