using System.Text.Json.Serialization;

namespace YallaFinance.Infrastructure.Providers.Nibo.Financial.Requests;

internal sealed record CreateNiboReceivableRequest(
    string StakeholderId,
    string Description,
    string Reference,
    DateOnly ScheduleDate,
    DateOnly DueDate,
    DateOnly AccrualDate,
    IReadOnlyList<CreateNiboReceivableCategoryRequest> Categories,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? CostCenterValueType = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<CreateNiboReceivableCostCenterRequest>? CostCenters = null);

internal sealed record CreateNiboReceivableCostCenterRequest(string CostCenterId, int Percent);
