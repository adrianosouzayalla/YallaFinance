import type { CSSProperties } from "react";
import { financeTokens } from "@/theme/designTokens";

type Props = {
  size?: number;
  color?: string;
  label?: string;
  showLabel?: boolean;
  className?: string;
  style?: CSSProperties;
};

/** CSS-only animation. No client boundary or animation dependency required. */
export function YallaCamelLoader({
  size = 80,
  color = financeTokens.colors.brandNavy,
  label = "Carregando...",
  showLabel = false,
  className = "",
  style,
}: Props) {
  return (
    <span
      className={`yallaLoader ${className}`}
      role="status"
      aria-live="polite"
      style={{ color, ...style }}
    >
      <svg
        viewBox="60 35 370 450"
        className="yallaCamel"
        width={size}
        height={(size * 450) / 370}
        style={{ color: "inherit" }}
        aria-hidden="true"
        focusable="false"
      >
        <g className="walkBody" fill="currentColor">
          <path
            className="leg farRear"
            d="M165 285 L199 300 L178 356 L177 455 L161 455 L157 355 Z"
          />
          <path
            className="leg farFront"
            d="M302 284 L331 282 L315 450 Q315 455 310 455 L295 455 L297 356 Z"
          />
          <path
            fillRule="evenodd"
            d="M116 280 L116 251 Q116 222 136 191 L195 104 Q208 84 223 105 L315 212 L316 79 Q299 90 290 77 Q286 71 286 62 L384 62 Q389 62 389 68 L389 102 Q388 117 375 110 L360 103 Q350 98 350 111 L356 204 Q363 270 321 292 Q299 307 271 306 L143 306 L130 280 Z M373 79 L377 77 L375 83 L372 83 Z"
          />
          <path
            className="leg nearRear"
            d="M130 273 L182 283 L155 354 L147 455 L130 456 L130 350 Z"
          />
          <path
            className="leg nearFront"
            d="M256 285 L291 290 L279 358 L271 455 L255 455 L255 354 Z"
          />
        </g>
      </svg>
      <span className={showLabel ? "yallaLoaderLabel" : "yallaSrOnly"}>{label}</span>
    </span>
  );
}
