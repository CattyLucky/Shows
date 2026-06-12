using System.Globalization;

namespace Content.Shared._NF.Bank;

public static class BankSystemExtensions
{
    public enum CurrencySymbolLocation
    {
        Default,
        Prefix,
        Suffix
    }

    private const int PrefixCurrencyPositivePattern = 0;
    private const int PrefixCurrencyNegativePattern = 1;
    private const int SuffixCurrencyPositivePattern = 3;
    private const int SuffixCurrencyNegativePattern = 8;

    public static string ToCurrencyString(
        long amount,
        CultureInfo? culture = null,
        string? symbolOverride = null,
        string? separatorOverride = null,
        CurrencySymbolLocation symbolLocation = CurrencySymbolLocation.Default)
    {
        culture ??= CultureInfo.CurrentCulture;
        var numberFormat = (NumberFormatInfo) culture.NumberFormat.Clone();

        if (symbolOverride != null)
            numberFormat.CurrencySymbol = symbolOverride;

        if (separatorOverride != null)
            numberFormat.CurrencyGroupSeparator = separatorOverride;

        switch (symbolLocation)
        {
            case CurrencySymbolLocation.Default:
                break;
            case CurrencySymbolLocation.Prefix:
                numberFormat.CurrencyPositivePattern = PrefixCurrencyPositivePattern;
                numberFormat.CurrencyNegativePattern = PrefixCurrencyNegativePattern;
                break;
            case CurrencySymbolLocation.Suffix:
                numberFormat.CurrencyPositivePattern = SuffixCurrencyPositivePattern;
                numberFormat.CurrencyNegativePattern = SuffixCurrencyNegativePattern;
                break;
        }

        return string.Format(numberFormat, "{0:C0}", amount);
    }

    public static string ToIndependentString(long amount, CultureInfo? culture = null)
    {
        return ToCurrencyString(amount, culture, symbolOverride: "", symbolLocation: CurrencySymbolLocation.Prefix);
    }

    public static string ToSpesoString(long amount, CultureInfo? culture = null)
    {
        return ToCurrencyString(amount, culture, symbolOverride: "$", symbolLocation: CurrencySymbolLocation.Prefix);
    }
}
