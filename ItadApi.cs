using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Calculator
{
	using static ApiCommon;

	public static class ItadApi
	{
		// Put your own ITAD app.
		internal const string API_KEY = "8446f2b9d09ff21e4336a40c48f024e2bbcdae92";

		/// <summary>
		/// Look up ITAD game IDs by their names
		/// </summary>
		/// <param name="gameNames">An array of game names</param>
		/// <returns>A dictionary of game names and their ITAD game IDs</returns>
		public static async Task<IDictionary<string, string>> LookUpGameId(HttpClient client, ICollection<string> gameNames)
		{
			var response = await client.PostAsync($"https://api.isthereanydeal.com/lookup/id/title/v1?key={API_KEY}", JsonContentOf(gameNames));
			await ThrowOnBadHttpStatus(response);
			var res = Serialization.FromJsonStream<OrdinalIgnoreCaseStringDictionary<string>>(await response.Content.ReadAsStreamAsync());

			return res;
		}

		/// <summary>
		/// Get price overview of the games
		/// </summary>
		/// <param name="input">A list of ITAD game ID categorized by their lookup shop</param>
		public static async Task<PriceOverview> PriceOverview(HttpClient client, IDictionary<ItadShop, ICollection<string>> input, string country)
		{
			// ITAD accepts at most 200 games per request.
			var shopToChunks = new Dictionary<ItadShop, List<List<string>>>();

			foreach (var pair in input)
			{
				ItadShop shop = pair.Key;
				var itadIds = pair.Value;
				var inputChunk = new List<string>();
				shopToChunks[shop] = new List<List<string>>();

				foreach (var id in itadIds)
				{
					if (inputChunk.Count == 200)
					{
						shopToChunks[shop].Add(inputChunk);
						inputChunk = new List<string>();
					}

					inputChunk.Add(id);
				}

				shopToChunks[shop].Add(inputChunk);
			}

			var tasks = new List<Task<PriceOverviewOutput>>();

			foreach (var pair in shopToChunks)
			{
				ItadShop shop = pair.Key;
				var itadIdChunks = pair.Value;
				foreach (var chunk in itadIdChunks)
				{
					var task = client.PostAsync($"https://api.isthereanydeal.com/games/overview/v2?key={API_KEY}&shops={(int)shop}&country={country}", JsonContentOf(chunk))
						.ContinueWith(async (t) =>
						{
							var response = t.Result;
							await ThrowOnBadHttpStatus(response);
							var resChunk = Serialization.FromJsonStream<PriceOverviewOutput>(await response.Content.ReadAsStreamAsync());

							return resChunk;
						}, TaskContinuationOptions.OnlyOnRanToCompletion)
						.Unwrap();

					tasks.Add(task);
				}
			}

			var priceChunks = await IfCancelledThenTimeout(Task.WhenAll(tasks));
			var res = new Dictionary<PriceKey, Price>();
			Currency currency = default;
			bool isCurrencyInitialized = false;

			foreach (var price in priceChunks.SelectMany(c => c.prices))
			{
				var key = new PriceKey
				{
					id = price.id,
					shop = ItadShopExtension.FromInt(price.current.shop.id),
				};

				double lowPrice = 0;
				double regular = price.current.price.amount;

				// For free games, lowest will return
				// the old prices when those games were
				// still paid.
				//
				// If the game was free from day 1, lowest
				// will be null.
				if (regular != 0 && !(price.lowest is null))
				{
					lowPrice = price.lowest.price.amount;
				}

				if (!isCurrencyInitialized)
				{
					Enum.TryParse(price.current.price.currency, out currency);
					isCurrencyInitialized = true;
				}

				res[key] = new Price
				{
					lowPrice = lowPrice,
					price = regular,
				};
			}

			return new PriceOverview
			{
				price = res,
				currency = currency,
			};
		}
	}

	public class PriceKey
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

	public struct Price
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

	public class PriceOverview
	{
		public IDictionary<PriceKey, Price> price;
		public Currency currency;
	}

	// prices might be empty (e.g., https://isthereanydeal.com/game/minecraft-bedrock/info/)
	public class PriceOverviewOutput
	{
		public Price[] prices;
		public class Price
		{
			public string id;
			public Current current;
			public Lowest lowest;
			
			public class Current
			{
				public Shop shop;
				public InnerPrice price;
			}

			public class Lowest
			{
				public InnerPrice price;
			}

			public class Shop
			{
				public int id;
			}

			public class InnerPrice
			{
				public double amount;
				public string currency;
			}
		}
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

	internal class HistoricalLow: Dictionary<PriceKey, Price>
	{
		public HistoricalLow() : base(new HistoricalLowComparator())
		{

		}

		private class HistoricalLowComparator : EqualityComparer<PriceKey>
		{
			public override bool Equals(PriceKey lhs, PriceKey rhs)
			{
				return lhs.id == rhs.id && lhs.shop == rhs.shop;
			}

			public override int GetHashCode(PriceKey key)
			{
				return key.GetHashCode();
			}
		}
	}
}