using Content.Client.Eui;
using Content.Shared._Reserve.Inventory.UI;
using Content.Shared.Eui;

namespace Content.Client._Reserve.Inventory.UI;

public sealed class InventoryEui : BaseEui
{
    private readonly InventoryWindow _window;

    public InventoryEui()
    {
        _window = new InventoryWindow();
        _window.OnClose += () => SendMessage(new InventoryEuiMsg.Close());
        _window.OnUseItem += itemId => SendMessage(new InventoryEuiMsg.UseItem { ItemId = itemId });
    }

    public override void Opened()
    {
        _window.OpenCentered();
    }

    public override void Closed()
    {
        _window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is InventoryEuiState s)
            _window.Populate(s);
    }
}
