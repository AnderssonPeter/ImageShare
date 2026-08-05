/**
 * UsageAgreementGate — proactive usage-agreement gate.
 *
 * Probes `GET /api/usage-agreement` once on app load (see
 * `useUsageAgreementQuery`) and, when the agreement is enabled and not yet
 * accepted (`accepted === false`), renders a blocking `UsageAgreementDialog`
 * over the app. The dialog is a portal-based modal (focus trap + scroll lock
 * + pointer-dismissal disabled) so the rest of the UI is non-interactive until
 * the user accepts; downloads and full-image serving also stay 403-gated on
 * the backend until then. The agreement is an optional server-side feature:
 * when disabled the endpoint returns 404 (see `isUsageAgreementDisabled`) and
 * the gate renders nothing.
 *
 * While the probe is in flight a full-screen `UsageAgreementLoading` overlay
 * is shown so the app does not flash browsable content (and let the user
 * start a download the backend would then 403) before the gate knows whether
 * the agreement is required. Once the query settles — accepted, disabled, or
 * a non-404 error handled by the global error handler — the overlay unmounts.
 *
 * Rendered as a sibling of the router (see `main.tsx`) rather than wrapping
 * it, so it adds no JSX nesting to the provider tree.
 */
import { isUsageAgreementDisabled, useUsageAgreementQuery } from "@lib/api/usageAgreement";
import { Loader2 } from "lucide-react";
import UsageAgreementDialog from "@components/UsageAgreementDialog";
import { tw } from "@lib/utils";

const LOADING_OVERLAY_CLASS = tw`fixed inset-0 z-50 flex items-center justify-center bg-background/80 backdrop-blur-xs`;
const LOADING_SPINNER_CLASS = tw`size-8 animate-spin text-muted-foreground`;

function UsageAgreementLoading(): React.JSX.Element {
  return (
    <div
      className={LOADING_OVERLAY_CLASS}
      role="status"
      aria-live="polite"
      data-testid="usage-agreement-loading"
    >
      <Loader2 className={LOADING_SPINNER_CLASS} />
    </div>
  );
}

export default function UsageAgreementGate(): React.JSX.Element | undefined {
  const { data, error, isLoading } = useUsageAgreementQuery();
  if (isLoading) {
    return <UsageAgreementLoading />;
  }
  // Feature disabled (404), errored (non-404 surfaced globally), or already accepted — don't gate.
  if (isUsageAgreementDisabled(error) || data === undefined || data.accepted) {
    return;
  }
  return <UsageAgreementDialog agreement={data} />;
}
