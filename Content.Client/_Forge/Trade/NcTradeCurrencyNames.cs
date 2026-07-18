using Content.Shared.Stacks;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client._Forge.Trade;

public static class NcTradeCurrencyNames
{
    public const string BankCreditCurrencyId = "BankCredit";

    public static string GetDisplayName(string? currencyId, IPrototypeManager prototypes)
    {
        if (string.IsNullOrWhiteSpace(currencyId))
            return string.Empty;

        if (currencyId == BankCreditCurrencyId &&
            Loc.TryGetString("nc-store-currency-bank-credit", out var bankCredits))
            return bankCredits;

        if (prototypes.TryIndex<StackPrototype>(currencyId, out var stackProto) &&
            prototypes.TryIndex<EntityPrototype>(stackProto.Spawn, out var currencyEnt))
            return currencyEnt.Name;

        return currencyId;
    }

    public static string GetShortDisplayName(string? currencyId, IPrototypeManager prototypes)
    {
        if (string.IsNullOrWhiteSpace(currencyId))
            return string.Empty;

        if (currencyId == BankCreditCurrencyId &&
            Loc.TryGetString("nc-store-currency-bank-credit-short", out var bankCredits))
            return bankCredits;

        return GetDisplayName(currencyId, prototypes);
    }

    public static bool TryGetIcon(
        string? currencyId,
        IPrototypeManager prototypes,
        SpriteSystem sprites,
        out Texture texture)
    {
        texture = default!;

        if (string.IsNullOrWhiteSpace(currencyId))
            return false;

        if (!prototypes.TryIndex<StackPrototype>(currencyId, out var stackProto) ||
            !prototypes.TryIndex<EntityPrototype>(stackProto.Spawn, out var entProto))
            return false;

        texture = sprites.GetPrototypeIcon(entProto).Default;
        return texture != null;
    }
}
