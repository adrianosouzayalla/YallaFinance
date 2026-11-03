using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

using YallaFinance.Application.Common.Context;
using YallaFinance.Application.Repositories.Fleet.Contracts;

namespace YallaFinance.Infrastructure.Mockup;

public static class JsonMockDataLoader
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    static JsonMockDataLoader() => Options.Converters.Add(new JsonStringEnumConverter());

    public static async Task<MockSeedData> LoadAsync(
        string dataDirectory,
        CancellationToken cancellationToken = default)
    {
        var movements = await ReadAsync<List<FleetMovementResponse>>(
            Path.Combine(
                dataDirectory,
                "fleet-movements.json"),
            cancellationToken);

        var normalizedMovements = movements.Select(item => item with
        {
            TenantId = NormalizeTenantId(item.TenantId),
            Id = NormalizeRecordId(item.Id),
            CustomerId = RequireCustomerId(item.CustomerId)
        }).ToArray();

        EnsureUniqueRecordIds(normalizedMovements.Select(item => item.Id), "fleet movement");
        return new MockSeedData([], [], normalizedMovements);
    }

    private static async Task<T> ReadAsync<T>(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream, Options, cancellationToken)
            ?? throw new InvalidDataException($"Fixture '{path}' is empty.");
    }

    private static void EnsureUniqueRecordIds(IEnumerable<string> recordIds, string recordType)
    {
        var duplicates = recordIds.GroupBy(recordId => recordId)
            .Where(recordGroup => recordGroup.Count() > 1)
            .Select(recordGroup => recordGroup.Key)
            .ToArray();
        if (duplicates.Length > 0)
            throw new InvalidDataException($"Duplicate {recordType} IDs: {string.Join(
                ", ",
                duplicates)}");
    }

    private static string RequireCustomerId(string? value) => Guid.TryParse(value, out _)
        ? value!
        : throw new InvalidDataException("Debt and movement records require a customerId UUID distinct from tenant identity.");

    private static string NormalizeTenantId(string? value) => Guid.TryParse(value, out _) ? value! : TenantConstants.YallaTenantId;

    private static string NormalizeRecordId(string value) => Guid.TryParse(value, out _) ? value : StableUuid(value);

    public static string StableUuid(string source)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"YallaFinance.Mockup:{source}"));
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x50);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes[..16]).ToString();
    }
}
