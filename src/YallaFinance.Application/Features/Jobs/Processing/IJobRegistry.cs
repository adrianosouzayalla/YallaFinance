using YallaFinance.Application;
namespace YallaFinance.Application.Features.Jobs.Processing;

public interface IJobRegistry
{
    bool Contains(string type);
    string GetTitle(string type);
    bool SupportsPayload<TPayload>(string type);
}
