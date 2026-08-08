import type en from "@lib/i18n/locales/en";

declare module "i18next" {
  interface CustomTypeOptions {
    defaultNS: "translation";
    strictKeyChecks: true;
    resources: {
      translation: typeof en;
    };
  }
}
