namespace Ogle
{
    public static class StringExtensions
    {
        public static string LowerCaseFirstCharacter(this string str)
        {
            return char.ToLower(str[0]) + str.Substring(1);
        }

        public static string? Truncate(this string? str, int maxLength)
        {
            if (str != null &&
                str.Length > maxLength)
            {
                return str.Substring(0, maxLength);
            }
            else
            {
                return str;
            }
        }
    }
}

