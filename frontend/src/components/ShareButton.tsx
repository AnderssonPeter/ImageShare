import Button from "@components/ui/Button";
import { Share } from "lucide-react";
import { cva } from "class-variance-authority";
import { tw } from "@lib/utils";
import { useTranslation } from "@lib/i18n";

const ICON_CLASS = tw`size-4`;
const OVERLAY_CLASS = tw`absolute top-2 left-2 z-10 rounded-full bg-black/50 text-white backdrop-blur-sm hover:bg-black/70`;

const shareButtonVariants = cva("", {
  variants: { variant: { "app-bar": "", overlay: OVERLAY_CLASS } },
  defaultVariants: { variant: "app-bar" },
});

interface ShareButtonProps {
  onClick: (() => void) | undefined;
  variant: "app-bar" | "overlay";
  ariaLabel?: string;
}

export default function ShareButton({
  onClick,
  variant,
  ariaLabel,
}: ShareButtonProps): React.JSX.Element | undefined {
  const { t: translate } = useTranslation();
  if (onClick === undefined) {
    return;
  }
  const resolvedAriaLabel = ariaLabel ?? translate("share.action");
  const className =
    variant === "app-bar"
      ? Button.buttonVariants({ variant: "ghost", size: "icon-sm" })
      : shareButtonVariants({ variant });
  return (
    <Button
      variant="ghost"
      className={className}
      size="icon-sm"
      onClick={onClick}
      aria-label={resolvedAriaLabel}
    >
      <Share className={ICON_CLASS} />
    </Button>
  );
}
