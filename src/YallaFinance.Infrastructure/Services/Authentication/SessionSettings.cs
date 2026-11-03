using System.Globalization;
using YallaFinance.Application.Services.Authentication.Errors;

namespace YallaFinance.Infrastructure.Services.Authentication;
public sealed record SessionSettings(int LifetimeMinutes)
{
    public static SessionSettings Read(IReadOnlyDictionary<string, string> values)
    {
        if (!values.TryGetValue("SessionLifetimeMinutes", out var text)
            || !int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var minutes)
            || minutes is < 1 or > 1440) throw new SessionConfigurationException();
        return new(minutes);
    }
}
