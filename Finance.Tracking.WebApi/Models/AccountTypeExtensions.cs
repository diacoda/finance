

public static class AccountTypeExtensions
{
    public static AccountFilter ToAccountFilter(this AccountType type) => type switch
    {
        AccountType.RRSP => AccountFilter.RRSP,
        AccountType.RRSPSpousal => AccountFilter.RRSP,
        AccountType.LIRAFederal => AccountFilter.LIRA,
        AccountType.LIRAProvincial => AccountFilter.LIRA,
        AccountType.RESP => AccountFilter.RESP,
        AccountType.TFSA => AccountFilter.TFSA,
        AccountType.NonReg => AccountFilter.NONREG,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
}