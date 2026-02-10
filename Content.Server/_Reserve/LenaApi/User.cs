namespace Content.Server._Reserve.LenaApi;

public sealed record User
{
    public ApiWrapper.UserRead UserRead;
    public List<ApiWrapper.Item> Items;

    public User(ApiWrapper.UserRead userRead, ApiWrapper.Inventory inventory)
    {
        UserRead = userRead;
        Items = inventory.Items.FindAll(item => item.CanBeUsed);
    }
}
