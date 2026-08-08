/**
 * ThemeToggle — app-bar control that switches between light and dark mode.
 *
 * A single icon button reflecting the effective theme: a sun when light, a
 * moon when dark. Clicking pins the opposite theme as an explicit override.
 *
 * The effective theme comes from the ThemeProvider context, which resolves
 * an explicit override > system preference > default. Clicking always pins
 * an override; the toggle does not expose a "follow system" mode.
 */
import { Moon, Sun } from "lucide-react";
import Button from "@components/ui/Button";
import Tooltip from "@components/ui/Tooltip";
import { useCallback } from "react";
import { useThemeContext } from "@lib/themeContext";
import { useTranslation } from "@lib/i18n";

interface ThemeToggleTriggerProps {
  isDark: boolean;
  ariaLabel: string;
  onToggle: () => void;
}

function ThemeToggleTrigger({ isDark, ariaLabel, onToggle }: ThemeToggleTriggerProps) {
  const Icon = isDark ? Moon : Sun;
  return (
    <Tooltip.TooltipTrigger
      className={Button.buttonVariants({ variant: "ghost", size: "icon" })}
      onClick={onToggle}
      aria-label={ariaLabel}
    >
      <Icon className="size-4" />
    </Tooltip.TooltipTrigger>
  );
}

export default function ThemeToggle(): React.JSX.Element {
  const { theme, setTheme } = useThemeContext();
  const { t: translate } = useTranslation();
  const isDark = theme === "dark";

  const handleToggle = useCallback(() => {
    setTheme(isDark ? "light" : "dark");
  }, [isDark, setTheme]);

  const switchLabel = isDark ? "theme.switchToLight" : "theme.switchToDark";
  const stateLabel = isDark ? "theme.dark" : "theme.light";

  return (
    <Tooltip.Tooltip>
      <ThemeToggleTrigger
        isDark={isDark}
        ariaLabel={translate(switchLabel)}
        onToggle={handleToggle}
      />
      <Tooltip.TooltipContent>{translate(stateLabel)}</Tooltip.TooltipContent>
    </Tooltip.Tooltip>
  );
}
