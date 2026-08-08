const en = {
  common: {
    brand: "ImageShare",
    user: "User",
  },
  theme: {
    switchToLight: "Switch to light theme",
    switchToDark: "Switch to dark theme",
    light: "Light theme",
    dark: "Dark theme",
  },
  language: {
    english: "English",
    swedish: "Svenska",
  },
  breadcrumb: {
    home: "Home",
  },
  content: {
    emptyFolder: "This folder is empty.",
  },
  download: {
    trigger: "Download folder",
    started: "Download started",
    title: "Download folder",
    description: "Choose the image format and whether to include subfolders.",
    recursive: "Recursive (include subfolders)",
    formatLegend: "Format",
    formats: {
      avif: "AVIF",
      webp: "WEBP",
      jpg: "JPG",
      all: "All formats",
    },
    formatDescriptions: {
      avif: "Smallest files. Modern format, limited application support.",
      webp: "Small files with limited application support.",
      jpg: "Universal compatibility, larger files.",
      all: "Downloads every available format.",
    },
    button: "Download",
  },
  share: {
    action: "Share",
    generateTitle: "Generate share link",
    generateDescription:
      "Create a time-limited link that grants access to folders matching the filter.",
    fields: {
      name: "Name",
      endDate: "End date",
      filter: "Filter",
      allFolders: "All folders",
      returnUrl: "Return URL",
      returnUrlPlaceholder: "Optional, e.g. /browse/photos",
    },
    errors: {
      nameRequired: "A name must be specified.",
      filterRequired: "A filter must be specified.",
      endDateRequired: "An end date must be specified.",
      dateInvalid: "Invalid date.",
      endDateFuture: "The end date must be in the future.",
    },
    submit: "Share",
    generating: "Sharing…",
    resultTitle: "Share link generated",
    resultDescription:
      "Share this link or scan the QR code to grant access to the matching folders.",
    copyLink: "Copy link",
    copied: "Copied",
    sendToEmail: "Send to email",
    sending: "Sending…",
    shareTitle: "ImageShare link",
    toasts: {
      shared: "Shared",
      emailError: "Could not send email",
      copied: "Link copied",
    },
  },
  imageViewer: {
    share: "Share image",
    close: "Close",
  },
  tiles: {
    openFolder: "Open folder {{name}}",
    openImage: "Open image {{name}}",
  },
  errors: {
    notFound: "This page could not be found.",
    goLibrary: "Go to library",
    retry: "Retry",
    redirecting: "Redirecting to sign in…",
    notFoundMessage: "We couldn't find what you were looking for.",
    unknown: "Something went wrong.",
  },
  usageAgreement: {
    title: "Usage agreement",
    description:
      "Please read and accept the agreement below to continue. You may be asked again if it changes.",
    acceptError: "Failed to accept the agreement. Please try again.",
    accept: "Accept",
    accepting: "Accepting…",
  },
} as const;

export default en;
