namespace Finance.Tracking.Extensions;

public static class SymbolExtensions
{
    /// <summary>
    /// Map Symbol enum values to their Yahoo Finance tickers
    /// </summary>
    public static string ToTicker(this Symbol symbol) => symbol switch
    {
        Symbol.VFV_TO => "VFV.TO",
        Symbol.VCE_TO => "VCE.TO",
        Symbol.HXQ_TO => "HXQ.TO",
        Symbol.BTCC_TO => "BTCC.TO",
        Symbol.MFC_TO => "MFC.TO",
        Symbol.PREF_TO => "PREF.TO",
        Symbol.BKCC_TO => "BKCC.TO",
        Symbol.TRI => "TRI",
        Symbol.ZGLD_TO => "ZGLD.TO",
        Symbol.VDY_TO => "VDY.TO",
        Symbol.VI_TO => "VI.TO",
        // TD e-Series do not exist on Yahoo
        Symbol.TDB900 or Symbol.TDB902 or Symbol.TDB911 =>
            throw new NotSupportedException($"Symbol {symbol} is not supported on Yahoo Finance"),
        _ => throw new ArgumentOutOfRangeException(nameof(symbol), symbol, null)
    };
}
