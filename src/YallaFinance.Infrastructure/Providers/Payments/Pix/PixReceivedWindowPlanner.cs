namespace YallaFinance.Infrastructure.Providers.Payments.Pix;

// Divide uma janela de recebimentos nos segmentos exigidos pelo limite declarado pelo provider.
// Os limites de mês são calculados no fuso de Brasília, onde o calendário do Pix é contado.
internal static class PixReceivedWindowPlanner
{
    private const string BusinessTimeZoneId = "America/Sao_Paulo";

    public static IReadOnlyList<PixReceivedWindow> Split(PixReceivedWindow window, PixReceivedWindowRule rule)
    {
        if (window.Start > window.End)
            return [];
        if (rule == PixReceivedWindowRule.Unrestricted)
            return [window];

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(BusinessTimeZoneId);
        var segments = new List<PixReceivedWindow>();
        var segmentStart = window.Start;
        while (segmentStart <= window.End)
        {
            var localStart = TimeZoneInfo.ConvertTime(segmentStart, timeZone).DateTime;
            var nextMonthLocal = new DateTime(localStart.Year, localStart.Month, 1, 0, 0, 0, DateTimeKind.Unspecified)
                .AddMonths(1);
            var nextMonth = new DateTimeOffset(nextMonthLocal, timeZone.GetUtcOffset(nextMonthLocal));
            // O fim do segmento e o ultimo instante do mes, para que inicio e fim fiquem no mesmo mes.
            var lastInstantOfMonth = nextMonth.AddTicks(-1);
            var segmentEnd = lastInstantOfMonth < window.End ? lastInstantOfMonth : window.End;
            segments.Add(new PixReceivedWindow(segmentStart, segmentEnd));
            segmentStart = nextMonth;
        }

        return segments;
    }
}
