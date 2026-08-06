import { type ChangeEvent, useCallback } from "react";
import Input from "@components/ui/Input";

interface ShareReturnUrlFieldProps {
  id: string;
  value: string;
  onChange: (value: string) => void;
  onBlur: () => void;
}

export default function ShareReturnUrlField({
  id,
  value,
  onChange,
  onBlur,
}: ShareReturnUrlFieldProps): React.JSX.Element {
  const handleInputChange = useCallback(
    (event: ChangeEvent<HTMLInputElement>) => onChange(event.target.value),
    [onChange],
  );
  return (
    <div className="flex flex-col gap-1">
      <Input
        id={id}
        type="text"
        value={value}
        onChange={handleInputChange}
        onBlur={onBlur}
        placeholder="Optional, e.g. /browse/photos"
        inputMode="url"
        aria-label="Return URL"
      />
    </div>
  );
}
