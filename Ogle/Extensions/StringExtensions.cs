namespace Ogle
{
	public static class StringExtensions
	{
		public static string LowerCaseFirstCharacter(this string str)
		{
			return char.ToLower(str[0]) + str.Substring(1);
		}
    }
}

