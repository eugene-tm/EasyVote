using EasyVote.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;

namespace EasyVote.Controllers
{
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;
		private readonly HttpClient _httpClient;

		private static DataArrayModel globalDataList;

		public HomeController(ILogger<HomeController> logger)
		{
			_logger = logger;
			_httpClient = new HttpClient();
		}




		public IActionResult Index()
		{
			return View();
		}



		public async Task<IActionResult> SecondPage(string topic, int itemCount)
		{
			if (topic == "1")
			{
				// we add 1 to the item count because we skip index 0 in the javascript when the vote screen is rendered
				var response = await _httpClient.GetAsync($"https://meowfacts.herokuapp.com/?count={itemCount}");
				response.EnsureSuccessStatusCode();

				var catFactJson = await response.Content.ReadAsStringAsync();

				// Parse the JSON response
				var catFactObject = JsonSerializer.Deserialize<JsonElement>(catFactJson);
				var catFactsList = catFactObject.GetProperty("data").EnumerateArray()
												.Select(item => item.GetString())
												.ToList();

				var dataList = new DataArrayModel();

				// Initialize the DataArray property with the appropriate size
				dataList.DataArray = new Data[catFactsList.Count];

				for (int i = 0; i < catFactsList.Count; i++)
				{
					var catFact = catFactsList[i];
					var dataObject = new Data
					{
						ItemData = catFact,
						ItemScore = 0
					};
					dataList.DataArray[i] = dataObject;
				}

				dataList.ArrayIndex = 0;

				globalDataList = dataList;


				return PartialView("_SecondPage", dataList);
			}
			else
			{
				return Error();
			}
		}

		public IActionResult UpdateScore(int dataIndex, int scoreChange)
		{
			var updatedData = globalDataList.DataArray[dataIndex];
			updatedData.ItemScore += scoreChange;

			// Save the index and the score to the session
			// HttpContext.Session.SetInt32($"ItemScore_{dataIndex}", updatedData.ItemScore);

			Console.WriteLine(globalDataList);

			return Json(updatedData);
		}

		public IActionResult GetFullList()
		{
			return Json(globalDataList);
		}

		
		public IActionResult GetItemAtIndex(int index)
		{
			return Json(globalDataList.DataArray[index]);
		}
		
		public IActionResult GetHighestScoresByIndex(int index)
		{
			var sortedArray = globalDataList.DataArray.OrderByDescending(data => data.ItemScore).ToArray();

			return Json(sortedArray[index]);
		}


		public IActionResult ResultsPage()
		{
			return PartialView("_ResultsPage");
		}



		public IActionResult FirstPage()
		{
			return PartialView("_Home");
		}


		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}