const sv = {
  common: {
    brand: "Image Share",
    user: "Användare",
  },
  theme: {
    switchToLight: "Byt till ljust tema",
    switchToDark: "Byt till mörkt tema",
    light: "Ljust tema",
    dark: "Mörkt tema",
  },
  language: {
    english: "English",
    swedish: "Svenska",
  },
  breadcrumb: {
    home: "Hem",
  },
  content: {
    emptyFolder: "Den här mappen är tom.",
  },
  download: {
    trigger: "Hämta mapp",
    started: "Nedladdning startad",
    title: "Hämta mapp",
    description: "Välj bildformat och om undermappar ska inkluderas.",
    recursive: "Rekursivt (inkludera undermappar)",
    formatLegend: "Format",
    formats: {
      avif: "AVIF",
      webp: "WEBP",
      jpg: "JPG",
      all: "Alla format",
    },
    formatDescriptions: {
      avif: "Minst filer. Modernt format, begränsat programstöd.",
      webp: "Små filer med begränsat programstöd.",
      jpg: "Universellt kompatibel, större filer.",
      all: "Hämtar alla tillgängliga format.",
    },
    button: "Hämta",
  },
  share: {
    action: "Dela",
    generateTitle: "Skapa delningslänk",
    generateDescription:
      "Skapa en tidsbegränsad länk som ger åtkomst till mappar som matchar filtret.",
    fields: {
      name: "Namn",
      endDate: "Slutdatum",
      filter: "Filter",
      allFolders: "Alla mappar",
      returnUrl: "URL för återgång",
      returnUrlPlaceholder: "Valfritt, t.ex. /browse/photos",
    },
    errors: {
      nameRequired: "Ett namn måste anges.",
      filterRequired: "Ett filter måste anges.",
      endDateRequired: "Ett slutdatum måste anges.",
      dateInvalid: "Ogiltigt datum.",
      endDateFuture: "Slutdatumet måste ligga i framtiden.",
    },
    submit: "Dela",
    generating: "Delar…",
    resultTitle: "Delningslänk skapad",
    resultDescription:
      "Dela den här länken eller skanna QR-koden för att ge åtkomst till matchande mappar.",
    copyLink: "Kopiera länk",
    copied: "Kopierad",
    sendToEmail: "Skicka till e-post",
    sending: "Skickar…",
    shareTitle: "ImageShare-länk",
    toasts: {
      shared: "Delad",
      emailError: "Det gick inte att skicka e-post",
      copied: "Länk kopierad",
    },
  },
  imageViewer: {
    share: "Dela bild",
    close: "Stäng",
  },
  tiles: {
    openFolder: "Öppna mapp {{name}}",
    openImage: "Öppna bild {{name}}",
  },
  errors: {
    notFound: "Den här sidan kunde inte hittas.",
    goLibrary: "Gå till bibliotek",
    retry: "Försök igen",
    redirecting: "Omdirigerar till inloggning…",
    notFoundMessage: "Vi kunde inte hitta det du letade efter.",
    unknown: "Något gick fel.",
  },
  usageAgreement: {
    title: "Användaravtal",
    description:
      "Läs och godkänn avtalet nedan för att fortsätta. Du kan bli ombedd igen om det ändras.",
    acceptError: "Det gick inte att godkänna avtalet. Försök igen.",
    accept: "Godkänn",
    accepting: "Godkänner…",
  },
} as const;

export default sv;
