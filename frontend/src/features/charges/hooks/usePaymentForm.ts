import { useForm, useWatch } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { paymentSchema } from "../schemas/charge.schema";
import type { PaymentInput } from "@/services/api/charges/charge.types";

export function usePaymentForm(defaultValues: PaymentInput) {
  const form = useForm<PaymentInput>({ defaultValues, resolver: zodResolver(paymentSchema) });
  const payment = useWatch({ control: form.control }) as PaymentInput;
  function setPayment(next: PaymentInput) {
    form.setValue("method", next.method);
    form.setValue("paymentMethodId", next.paymentMethodId);
    form.setValue("dueDate", next.dueDate);
    form.setValue("customerNote", next.customerNote);
  }
  return { ...form, payment, setPayment };
}
