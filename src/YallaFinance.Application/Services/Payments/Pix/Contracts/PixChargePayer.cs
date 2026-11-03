using YallaFinance.Application;
namespace YallaFinance.Application.Services.Payments.Pix.Contracts;

public sealed record PixChargePayer(string TaxId, string Name, Guid? CustomerId = null)
{
    public override string ToString() => "PixChargePayer [redacted]";
}
