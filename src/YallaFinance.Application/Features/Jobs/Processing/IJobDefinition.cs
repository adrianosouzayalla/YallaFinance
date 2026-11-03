using YallaFinance.Application;
namespace YallaFinance.Application.Features.Jobs.Processing;

public interface IJobDefinition<TPayload>
{
    string Type
    {
        get;
    }
    string BuildKey();
    TPayload BuildPayload();
}
