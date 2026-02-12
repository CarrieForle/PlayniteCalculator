using Playnite.SDK.Data;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Calculator
{
	internal static class ApiCommon
	{
		internal static async Task<T> TryParse<T>(HttpResponseMessage response, string msg)
			where T : class
		{
			string content = await response.Content.ReadAsStringAsync();

			if (!Serialization.TryFromJson(content, out T res))
			{
				throw new CalculatorException($"{msg}: {content}");
			}

			return res;
		}

		internal static async Task ThrowOnBadHttpStatus(HttpResponseMessage response)
		{
			if (response.IsSuccessStatusCode)
			{
				return;
			}

			string responseContent = await response.Content.ReadAsStringAsync();
			throw new HttpRequestException($"Request response is not OK [{response.StatusCode:d} {response.StatusCode}] \"{responseContent}\"");
		}

		internal static StringContent JsonContentOf<T>(T data)
			where T : class
		{
			return new StringContent(Serialization.ToJson(data), Encoding.UTF8, "application/json");
		}

		// HttpClient throws TaskCanceledException instead
		// of TimeoutException.
		// https://stackoverflow.com/questions/10547895/how-can-i-tell-when-httpclient-has-timed-out
		public static async Task<T> IfCancelledThenTimeout<T>(Task<T> task)
		{
			try
			{
				return await task;
			}
			catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException exi)
			{
				throw exi;
			}
			catch
			{
				throw;
			}
		}
	}
}
