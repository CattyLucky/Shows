using System.Numerics;
using Content.Shared._NF.Bank;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Lobby.UI.Loadouts;

public sealed partial class LoadoutContainer
{
    private static readonly Color ForgePriceBack = Color.FromHex("#202126");
    private static readonly Color ForgePriceBorder = Color.FromHex("#3A3D46");
    private static readonly Color ForgePriceText = Color.FromHex("#D6C79B");
    private static readonly Color ForgePriceUnavailableText = Color.FromHex("#B77A74");

    private PanelContainer? _forgePriceFrame;
    private Label? _forgePriceLabel;

    public void ForgeSetNameAndPrice(string itemName, int price, int balance, bool selected)
    {
        Text = itemName;
        Select.TextAlign = Label.AlignMode.Left;
        Select.ClipText = true;

        if (price <= 0)
        {
            SetForgePriceVisible(false);
            return;
        }

        EnsureForgePriceBadge();

        var priceValue = BankSystemExtensions.ToIndependentString(price).Trim();
        _forgePriceLabel!.Text = Loc.GetString("forge-loadout-price-badge", ("price", priceValue));
        _forgePriceLabel.FontColorOverride = selected || balance >= price
            ? ForgePriceText
            : ForgePriceUnavailableText;

        SetForgePriceVisible(true);
    }

    private void EnsureForgePriceBadge()
    {
        if (_forgePriceFrame != null)
            return;

        _forgePriceLabel = new Label
        {
            HorizontalAlignment = HAlignment.Right,
            VerticalAlignment = VAlignment.Center,
            Margin = new Thickness(7, 2, 7, 2),
        };

        _forgePriceFrame = new PanelContainer
        {
            MinSize = new Vector2(78, 0),
            Margin = new Thickness(0, 0, 5, 0),
            VerticalAlignment = VAlignment.Center,
            MouseFilter = MouseFilterMode.Ignore,
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = ForgePriceBack,
                BorderColor = ForgePriceBorder,
                BorderThickness = new Thickness(1),
            },
        };
        _forgePriceFrame.AddChild(_forgePriceLabel);

        AddChild(_forgePriceFrame);
        _forgePriceFrame.SetPositionInParent(Math.Min(ChildCount - 2, 1));
    }

    private void SetForgePriceVisible(bool visible)
    {
        if (_forgePriceFrame != null)
            _forgePriceFrame.Visible = visible;
    }
}
