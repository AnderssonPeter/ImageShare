/**
 * LanguageToggle — app-bar control that switches between English and Swedish.
 *
 * A single text button showing the code of the language a click will switch to
 * (`SV` when English is active, `EN` when Swedish is), so it reads as the
 * target rather than the current state. Clicking pins the other language as an
 * explicit override (persisted via the `LanguageProvider`).
 */
import Button from "@components/ui/Button";
import Tooltip from "@components/ui/Tooltip";
import { useCallback } from "react";
import { type Language, useTranslation } from "@lib/i18n";
import { useLanguageContext } from "@lib/i18nContext";

const OTHER_LANGUAGE: Record<Language, Language> = { en: "sv", sv: "en" };
const LANGUAGE_CODE: Record<Language, string> = { en: "EN", sv: "SV" };
const LANGUAGE_LABEL_KEY: Record<Language, "language.english" | "language.swedish"> = {
  en: "language.swedish",
  sv: "language.english",
};

export default function LanguageToggle(): React.JSX.Element {
  const { language, setLanguage } = useLanguageContext();
  const { t: translate } = useTranslation();
  const target = OTHER_LANGUAGE[language];
  const targetLabel = translate(LANGUAGE_LABEL_KEY[language]);

  const handleToggle = useCallback(() => {
    setLanguage(target);
  }, [setLanguage, target]);

  return (
    <Tooltip.Tooltip>
      <Tooltip.TooltipTrigger
        className={Button.buttonVariants({ variant: "ghost", size: "icon-sm" })}
        onClick={handleToggle}
        aria-label={targetLabel}
      >
        <span className="text-xs font-semibold">{LANGUAGE_CODE[language]}</span>
      </Tooltip.TooltipTrigger>
      <Tooltip.TooltipContent>{targetLabel}</Tooltip.TooltipContent>
    </Tooltip.Tooltip>
  );
}
