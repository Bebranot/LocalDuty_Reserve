using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Content.Server._Reserve.LenaApi;

public sealed partial class ApiWrapper
{
    public Task<Result<Inventory>> GetInventory(int id)
    {
        return Send<Inventory>(() => _httpClient.GetAsync("v1/inventory/get/" + id));
    }

    public Task<Result<BalanceModify>> PostEditBalance(int id, BalanceModify balanceModify)
    {
        return Send<BalanceModify>(() => _httpClient.PostAsJsonAsync($"v1/inventory/editBalance/{id}", balanceModify, _jsonSerializerOptions));
    }

    public record Inventory(
        int UserId,
        List<Item> Items,
        int TotalItems
    );

    public record Item(
        int Id,
        string ItemId,
        string ItemName,
        string ItemImageUrl,
        Rarity Rarity,
        string? Description,
        bool AvailableForPurchase,
        int? Price,
        string? Currency,
        bool CanBeUsed
    );

    // TODO: Needs actual naming
    public enum Rarity
    {
        Common = 1,
        Uncommon = 2,
        Rare = 3,
        Epic = 4,
        Legendary = 5,
        Unique = 6
    }

    public record BalanceModify
    {
        public int? ReserveCoins { get; init; }
        public int? DonateCoins { get; init; }
    }
}
