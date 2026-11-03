import { Box, Typography } from "@mui/material";
import { PrototypePanel } from "@/components/ui/PrototypePanel";
import { financeTokens } from "@/theme/designTokens";
export function JobJsonPanel({
  title,
  value,
  emptyMessage,
}: {
  title: string;
  value: unknown;
  emptyMessage: string;
}) {
  return (
    <PrototypePanel title={title}>
      <Box sx={{ px: 2.5, py: 2 }}>
        {value == null ? (
          <Typography color="text.secondary">{emptyMessage}</Typography>
        ) : (
          <Box
            component="pre"
            sx={{
              m: 0,
              width: "100%",
              whiteSpace: "pre-wrap",
              overflowWrap: "anywhere",
              fontFamily: "monospace",
              fontSize: financeTokens.typography.secondary,
              lineHeight: 1.7,
            }}
          >
            {JSON.stringify(value, null, 2)
              .split("\n")
              .map((line, index) => {
                const property = /^(\s*)("(?:\\.|[^"\\])*")(:)(.*)$/.exec(line);
                return (
                  <span key={index}>
                    {index > 0 ? "\n" : ""}
                    {property ? (
                      <>
                        {property[1]}
                        <strong>
                          {property[2]}
                          {property[3]}
                        </strong>
                        {property[4]}
                      </>
                    ) : (
                      line
                    )}
                  </span>
                );
              })}
          </Box>
        )}
      </Box>
    </PrototypePanel>
  );
}
