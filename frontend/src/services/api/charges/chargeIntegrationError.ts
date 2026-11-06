import { ApiError } from "@/services/api/apiError";
import type { Charge } from "@/services/api/charges/charge.types";

export class ChargeIntegrationError extends ApiError {
  constructor(
    public readonly charge: Charge,
    message: string,
  ) {
    super(
      charge.processing?.errorCode ??
        charge.financialIntegration?.errorCode ??
        "FINANCIAL_INTEGRATION_FAILED",
      message,
      200,
    );
    this.name = "ChargeIntegrationError";
  }
}
