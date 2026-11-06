import { useCallback, useEffect, useReducer, useRef, useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { publicPaymentApi } from "@/services/api/payments/publicPaymentApi.client";
import type { PublicPaymentCheckout } from "@/services/api/payments/payment.types";
import {
  checkoutFlowReducer,
  createInitialFlowState,
  expirationTimesOf,
  hasOpenAttempt,
  isPaymentReceived,
  isSameAttemptState,
  isSameCheckoutStatus,
} from "./checkoutFlow";
import { useCheckoutClock } from "./useCheckoutClock";
import { pollingIntervalFor, usePaymentStatusRefresh } from "./usePaymentStatusRefresh";

export type RefreshReason = "checkout" | "payment";

type PublicPaymentCheckoutInput = {
  token: string;
  checkout: PublicPaymentCheckout | null;
  temporaryError?: boolean;
};

export function usePublicPaymentCheckout({
  token,
  checkout: incomingCheckout,
  temporaryError = false,
}: PublicPaymentCheckoutInput) {
  const router = useRouter();
  const [lastCheckout, setLastCheckout] = useState(incomingCheckout);
  const checkout = incomingCheckout ?? lastCheckout;
  if (incomingCheckout && incomingCheckout !== lastCheckout) setLastCheckout(incomingCheckout);
  const [flow, dispatch] = useReducer(checkoutFlowReducer, checkout ?? null, (initialCheckout) =>
    createInitialFlowState(initialCheckout, Date.now()),
  );
  const attempt = flow.attemptOverride ?? checkout?.attempt ?? null;
  const paymentReceived = isPaymentReceived(checkout ?? null, attempt);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [attemptError, setAttemptError] = useState("");
  const submitting = useRef(false);
  const cardCommandKey = useRef<string | null>(null);
  const [cardOutcomePending, setCardOutcomePending] = useState(false);
  const now = useCheckoutClock(expirationTimesOf(checkout ?? null, attempt));
  const [isRefreshing, startRefresh] = useTransition();
  const [refreshReason, setRefreshReason] = useState<RefreshReason>("checkout");

  // Background check: a light status request. The full checkout is reloaded only when the status changed,
  // and no new check starts while one is running or while a reload has not yet delivered new data.
  const statusCheckInFlight = useRef(false);
  const fullReloadPending = useRef(false);
  useEffect(() => {
    fullReloadPending.current = false;
  }, [incomingCheckout]);
  const checkPaymentStatus = useCallback(async () => {
    if (statusCheckInFlight.current || fullReloadPending.current) return;
    statusCheckInFlight.current = true;
    try {
      const status = await publicPaymentApi.getStatus(token);
      if (checkout && isSameCheckoutStatus(status, checkout)) return;
      fullReloadPending.current = true;
      router.refresh();
    } catch {
      // The data on screen stays; the next background tick tries again.
    } finally {
      statusCheckInFlight.current = false;
    }
  }, [token, checkout, router]);
  usePaymentStatusRefresh({
    refresh: checkPaymentStatus,
    refreshWhenVisible: !paymentReceived && Boolean(checkout),
    pollingIntervalMs:
      !paymentReceived && hasOpenAttempt(attempt, now)
        ? pollingIntervalFor(attempt?.instrumentType)
        : null,
  });

  // The server attempt is external input: when it differs from the one the flow observed, the flow
  // records it through its own reducer. The reducer ignores an attempt it has already observed.
  if (!isSameAttemptState(flow.observedServerAttempt, checkout?.attempt)) {
    dispatch({ type: "tentativa_do_servidor_mudou", serverAttempt: checkout?.attempt, now });
  }

  const stepHeadingRef = useRef<HTMLHeadingElement>(null);
  const previousStep = useRef(flow.step);
  useEffect(() => {
    if (previousStep.current === flow.step) return;
    previousStep.current = flow.step;
    stepHeadingRef.current?.focus();
  }, [flow.step]);

  return {
    attempt,
    attemptError,
    cardCommandKey,
    cardOutcomePending,
    checkPaymentStatus,
    checkout,
    dispatch,
    flow,
    fullReloadPending,
    incomingCheckout,
    isRefreshing,
    isSubmitting,
    lastCheckout,
    now,
    paymentReceived,
    previousStep,
    refreshReason,
    router,
    setAttemptError,
    setCardOutcomePending,
    setIsSubmitting,
    setLastCheckout,
    setRefreshReason,
    startRefresh,
    statusCheckInFlight,
    stepHeadingRef,
    submitting,
    temporaryError,
    token,
  };
}
