using System.Collections.Generic;
using Finance.Tracking.Domain.Enums;

namespace Finance.Tracking.Domain;

public static class SymbolToAssetClass
{
    public static readonly Dictionary<Symbol, AssetClass> Map = new()
    {
        // US Stocks
        [Symbol.VFV_TO] = AssetClass.USStock,
        [Symbol.TDB900] = AssetClass.USStock,
        [Symbol.HXQ_TO] = AssetClass.USStock,

        // Canadian Stocks
        [Symbol.VCE_TO] = AssetClass.CanadianStock,
        [Symbol.VDY_TO] = AssetClass.CanadianStock,
        [Symbol.TRI] = AssetClass.CanadianStock,
        [Symbol.MFC_TO] = AssetClass.CanadianStock,
        [Symbol.BKCC_TO] = AssetClass.CanadianStock,
        [Symbol.PREF_TO] = AssetClass.CanadianStock,
        [Symbol.TDB902] = AssetClass.CanadianStock,

        // Developed ex-North America
        [Symbol.TDB911] = AssetClass.DevelopedStock,
        [Symbol.VI_TO] = AssetClass.DevelopedStock,

        // Bonds

        // Commodities
        [Symbol.ZGLD_TO] = AssetClass.Gold,
        [Symbol.ZGLH_TO] = AssetClass.Gold,

        // Crypto
        [Symbol.BTCC_TO] = AssetClass.Cryptocurrency,

        // Cash
        [Symbol.CASH] = AssetClass.Cash
        // Fallback
        // Any unmapped symbol = Other
    };

    public static AssetClass Resolve(Symbol symbol) =>
        Map.TryGetValue(symbol, out var assetClass) ? assetClass : AssetClass.Other;
}
