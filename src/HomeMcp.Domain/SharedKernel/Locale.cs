namespace HomeMcp.Domain.SharedKernel;

public static class Locale
{
    public static string ToLanguageCode(string locale) =>
        locale.Split('-')[0].ToLowerInvariant();
}
