using System.Net;

namespace Ogle.Extensions
{
	public static class HttpStatusCodeExtensions
	{
		public static bool IsSuccessCode(this HttpStatusCode statusCode)
		{
			return (int)statusCode >= 200 && (int)statusCode <= 299;
		}
	}
}

