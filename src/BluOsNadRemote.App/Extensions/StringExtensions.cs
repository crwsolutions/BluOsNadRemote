namespace BluOsNadRemote.App.Extensions;

internal static class StringExtensions
{
    internal static string Interpolate(this string s, int arg)
    {
        return string.Format(s, arg);
    }

    internal static string Interpolate(this string s, string arg)
    {
        return string.Format(s, arg);
    }
}
