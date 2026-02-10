using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Calculator
{
	public static class ItadApi
	{
		// Put your own ITAD app.
		internal const string API_KEY = "8446f2b9d09ff21e4336a40c48f024e2bbcdae92";

		// Use one HttpClient accross every class.
		internal static readonly HttpClient Client = new HttpClient()
		{
			Timeout = TimeSpan.FromSeconds(10)
		};

		/// <summary>
		/// Look up ITAD game IDs by their names
		/// </summary>
		/// <param name="gameNames">An array of game names</param>
		/// <returns>A dictionary of game names and their ITAD game IDs</returns>
		public static async Task<IDictionary<string, string>> LookUpGameId(ICollection<string> gameNames)
		{
			var response = await Client.PostAsync($"https://api.isthereanydeal.com/lookup/id/title/v1?key={API_KEY}", JsonContentOf(gameNames));
			await ThrowOnBadHttpStatus(response);
			var res = Serialization.FromJsonStream<OrdinalIgnoreCaseStringDictionary<string>>(await response.Content.ReadAsStreamAsync());

			return res;
		}

		/// <summary>
		/// Get historical low price of the games
		/// </summary>
		/// <param name="input">A list of ITAD game ID categorized by their lookup shop</param>
		/// <returns>A dictionary of games' ITAD IDs and their price info</returns>
		public static async Task<IDictionary<HistoricalLowKey, HistoricalLowOutput>> HistoricalLow(IDictionary<ItadShop, ICollection<string>> input, string country)
		{
			// ITAD accepts at most 200 games per request.
			var historicalLowInputs = new Dictionary<ItadShop, List<List<string>>>();

			foreach (var pair in input)
			{
				ItadShop shop = pair.Key;
				var itadIds = pair.Value;
				var inputChunk = new List<string>();
				historicalLowInputs[shop] = new List<List<string>>();

				foreach (var id in itadIds)
				{
					if (inputChunk.Count == 200)
					{
						historicalLowInputs[shop].Add(inputChunk);
						inputChunk = new List<string>();
					}

					inputChunk.Add(id);
				}

				historicalLowInputs[shop].Add(inputChunk);
			}

			var tasks = new List<Task<HistoricalLowOutputDeserialized[]>>();

			foreach (var pair in historicalLowInputs)
			{
				ItadShop shop = pair.Key;
				var itadIdChunks = pair.Value;
				foreach (var chunk in itadIdChunks)
				{
					var task = Client.PostAsync($"https://api.isthereanydeal.com/games/storelow/v2?key={API_KEY}&shops={(int)shop}&country={country}", JsonContentOf(chunk))
						.ContinueWith(async (t) =>
						{
							var response = t.Result;
							await ThrowOnBadHttpStatus(response);
							var resChunk = Serialization.FromJsonStream<HistoricalLowOutputDeserialized[]>(await response.Content.ReadAsStreamAsync());

							return resChunk;
						}, TaskContinuationOptions.OnlyOnRanToCompletion)
						.Unwrap();

					tasks.Add(task);
				}
			}

			var historyChunks = await Task.WhenAll(tasks);
			var res = new Dictionary<HistoricalLowKey, HistoricalLowOutput>();

			foreach (var chunk in historyChunks)
			{
				foreach (var history in chunk)
				{
					var key = new HistoricalLowKey
					{
						id = history.id,
						shop = ItadShopExtension.FromInt(history.lows[0].shop.id),
					};
					res[key] = new HistoricalLowOutput
					{
						lowPrice = history.lows[0].price.amount,
						price = history.lows[0].regular.amount,
					};
				}
			}

			return res;
		}

		internal static async Task<T> TryParse<T>(HttpResponseMessage response, string msg) 
			where T: class
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

		private static StringContent JsonContentOf<T>(T data)
			where T : class
		{
			return new StringContent(Serialization.ToJson(data), Encoding.UTF8, "application/json");
		}
	}

	public class HistoricalLowOutputDeserialized
	{
		public string id;
		public Low[] lows;

		public class Low
		{
			public Shop shop;

			public class Shop
			{
				public int id;
			}

			public Price price;
			public Price regular;

			public class Price
			{
				public double amount;
			}
		}
	}

	public class HistoricalLowKey
	{
		/// <summary>
		/// Game ITAD ID
		/// </summary>
		public string id;

		public ItadShop? shop;

		public override int GetHashCode()
		{
			int hash = 11;
			hash = 37 * hash + (id?.GetHashCode() ?? 0);
			hash = 37 * hash + (shop?.GetHashCode() ?? 0);

			return hash;
		}
	}

	public struct HistoricalLowOutput
	{
		/// <summary>
		/// Historical price
		/// </summary>
		public double lowPrice;

		/// <summary>
		/// Regular price
		/// </summary>
		public double price;
	}

	// The number is shopId which was gotten from https://api.isthereanydeal.com/service/shops/v1
	// It should be null for library that cannot be mapped
	// to ITAD shop.
	public enum ItadShop
	{
		Blizzard = 4,
		Ea = 52,
		Epic = 16,
		Gog = 35,
		HumbleBundle = 18,
		Indiegala = 42,
		Steam = 61,
		Ubisoft = 62,
		MicrosoftStore = 48,
	}

	public class ItadShopExtension
	{
		/// <summary>
		/// Map GameSource to ItadShop.
		/// </summary>
		/// <param name="source"></param>
		/// <returns>ItadShop or null if source cannot map to shops on ITAD</returns>
		public static ItadShop? FromGameSource(GameSource source)
		{
			switch (source?.Name)
			{
				case "Battle.net":
					return ItadShop.Blizzard;
				case "EA app":
					return ItadShop.Ea;
				case "Epic":
					return ItadShop.Epic;
				case "GOG":
					return ItadShop.Gog;
				case "Humble":
					return ItadShop.HumbleBundle;
				case "Indiegala":
					return ItadShop.Indiegala;
				case "Steam":
					return ItadShop.Steam;
				case "Ubisoft Connect":
					return ItadShop.Ubisoft;
				case "Xbox":
					return ItadShop.MicrosoftStore;
				default:
					return null;
			}
		}

		public static ItadShop? FromInt(int id)
		{
			if (Enum.IsDefined(typeof(ItadShop), id))
			{
				return (ItadShop)id;
			}

			return null;
		}
	}

	internal class OrdinalIgnoreCaseStringDictionary<TValue> : Dictionary<string, TValue>
	{
		public OrdinalIgnoreCaseStringDictionary() : base(StringComparer.OrdinalIgnoreCase)
		{
			
		}
	}

	internal class HistoricalLow: Dictionary<HistoricalLowKey, HistoricalLowOutput>
	{
		public HistoricalLow() : base(new HistoricalLowComparator())
		{

		}

		private class HistoricalLowComparator : EqualityComparer<HistoricalLowKey>
		{
			public override bool Equals(HistoricalLowKey lhs, HistoricalLowKey rhs)
			{
				return lhs.id == rhs.id && lhs.shop == rhs.shop;
			}

			public override int GetHashCode(HistoricalLowKey key)
			{
				return key.GetHashCode();
			}
		}
	}
}