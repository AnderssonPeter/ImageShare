import { type ChangeEvent, useCallback } from "react";
import cn from "@lib/utils";

interface RadioOption {
  value: string;
  label: string;
  description?: string;
}

interface RadioGroupProps {
  value: string;
  onChange: (value: string) => void;
  options: readonly RadioOption[];
  name: string;
  legend: string;
  className?: string;
}

interface RadioOptionRowProps {
  option: RadioOption;
  name: string;
  checked: boolean;
  onChange: (event: ChangeEvent<HTMLInputElement>) => void;
}

function RadioOptionRow({
  option,
  name,
  checked,
  onChange,
}: RadioOptionRowProps): React.JSX.Element {
  return (
    <label className="flex items-start gap-2 text-sm">
      <input
        type="radio"
        name={name}
        value={option.value}
        checked={checked}
        onChange={onChange}
        className="mt-0.5"
      />
      <span className="flex flex-col">
        {option.label}
        {option.description !== undefined && (
          <span className="text-xs text-muted-foreground">{option.description}</span>
        )}
      </span>
    </label>
  );
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
        <RadioOptionRow
          key={option.value}
          option={option}
          name={name}
          checked={value === option.value}
          onChange={handleChange}
        />
      ))}
    </fieldset>
  );
}

export default RadioGroup;
