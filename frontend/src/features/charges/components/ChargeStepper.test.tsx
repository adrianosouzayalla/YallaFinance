import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it } from "vitest";
import { ChargeStepper } from "@/features/charges/components/ChargeStepper";

afterEach(cleanup);

describe("ChargeStepper", () => {
  it("shows the three charging stages with the first stage active", () => {
    render(<ChargeStepper activeStep={0} />);

    expect(screen.getByText("1. Dados da cobrança")).toBeInTheDocument();
    expect(screen.getByText("2. Revisão e forma de cobrança")).toBeInTheDocument();
    expect(screen.getByText("3. Confirmação")).toBeInTheDocument();
    expect(
      screen.getByRole("listitem", { name: /Dados da cobrança, etapa atual/i }),
    ).toBeInTheDocument();
  });

  it("accepts the direct charge stages and identifies the current step when advancing", () => {
    const steps = ["Cliente", "Débitos", "Revisão", "Confirmação"];
    const { rerender } = render(
      <ChargeStepper
        activeStep={0}
        steps={steps}
        ariaLabel="Etapas da Cobrança Direta"
        responsive
      />,
    );

    expect(screen.getByRole("list", { name: "Etapas da Cobrança Direta" })).toBeInTheDocument();
    expect(screen.getByRole("listitem", { name: "Cliente, etapa atual" })).toHaveAttribute(
      "aria-current",
      "step",
    );
    expect(screen.getByText("2. Débitos")).toBeInTheDocument();

    rerender(
      <ChargeStepper
        activeStep={1}
        steps={steps}
        ariaLabel="Etapas da Cobrança Direta"
        responsive
      />,
    );

    expect(screen.getByRole("listitem", { name: "Débitos, etapa atual" })).toHaveAttribute(
      "aria-current",
      "step",
    );
    expect(screen.getByRole("listitem", { name: "Cliente" })).not.toHaveAttribute("aria-current");
  });
});
