using EasyVote.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Configuration;


namespace EasyVote.Controllers
{
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;
		private readonly HttpClient _httpClient;
		private readonly IConfiguration _configuration;

		private static DataArrayModel globalDataList;



		public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
		{
			_logger = logger;
			_httpClient = new HttpClient();
			_configuration = configuration;
		}




		public IActionResult Index()
		{
			return View();
		}



		public async Task<IActionResult> SecondPage(string topic, int itemCount)
		{
			string appData = null;

			var apiKey = _configuration["ApiNinjasApiKey"];

			var httpClient = new HttpClient();

			httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

			// Switch statement here is used to hit the correct API based on user input and obtain data for the rest of the program.
			switch (topic)
			{
				case "1":
					// Manually create a JSON object
					appData = @"
					[
						""TomCat"",
						""Greaser"",
						""CrowBar"",
						""Wooly Mammoth"",
						""Rics Backyard""
					]";

					break;

				case "2":

					string endpoint = $"https://api.api-ninjas.com/v1/babynames?gender=boy";
					var response = await httpClient.GetAsync(endpoint);
					response.EnsureSuccessStatusCode();

					if (!response.IsSuccessStatusCode)
					{
						return BadRequest("API data retrieval failed");
					}

					// setting our JSON data to a global variable to be accessed and used below
					appData = await response.Content.ReadAsStringAsync();
					break;

				case "3":
					// Hitting our cat facts API with an item count value 
					var response2 = await _httpClient.GetAsync($"https://meowfacts.herokuapp.com/?count={itemCount}");
					response2.EnsureSuccessStatusCode();

					if (!response2.IsSuccessStatusCode)
					{
						return BadRequest("API data retrieval failed");
					}

					// setting our JSON data to a global variable to be accessed and used below
					appData = await response2.Content.ReadAsStringAsync();

					break;
			}


			// Parse the JSON response
			var jsonObject = JsonSerializer.Deserialize<JsonElement>(appData);
var jsonDataList = JsonSerializer.Deserialize<List<string>>(appData);


			// set our data to an new instance of our data array model
			var dataList = new DataArrayModel();

			// Initialize the DataArray property with the appropriate size
			dataList.DataArray = new Data[jsonDataList.Count];

			for (int i = 0; i < jsonDataList.Count; i++)
			{
				var singleJsonObject = jsonDataList[i];
				var dataObject = new Data
				{
					ItemData = singleJsonObject,
					ItemScore = 0
				};
				dataList.DataArray[i] = dataObject;
			}

			dataList.ArrayIndex = 0;

			globalDataList = dataList;

			return PartialView("_SecondPage", dataList);




			//if (topic == "1")
			//{
			//	var response = await _httpClient.GetAsync($"https://meowfacts.herokuapp.com/?count={itemCount}");
			//	response.EnsureSuccessStatusCode();

			//	var jsonDataFromAPI = await response.Content.ReadAsStringAsync();

			//	// Parse the JSON response
			//	var jsonObject = JsonSerializer.Deserialize<JsonElement>(jsonDataFromAPI);
			//	var jsonDataList = jsonObject.GetProperty("data").EnumerateArray()
			//									.Select(item => item.GetString())
			//									.ToList();

			//	// set our data to an new instance of our data array model
			//	var dataList = new DataArrayModel();

			//	// Initialize the DataArray property with the appropriate size
			//	dataList.DataArray = new Data[jsonDataList.Count];

			//	for (int i = 0; i < jsonDataList.Count; i++)
			//	{
			//		var singleJsonObject = jsonDataList[i];
			//		var dataObject = new Data
			//		{
			//			ItemData = singleJsonObject,
			//			ItemScore = 0
			//		};
			//		dataList.DataArray[i] = dataObject;
			//	}

			//	dataList.ArrayIndex = 0;

			//	globalDataList = dataList;

			//	return PartialView("_SecondPage", dataList);
			//}
			//else
			//{
			//	return Error();
			//}
		}

		public IActionResult UpdateScore(int dataIndex, int scoreChange)
		{
			var updatedData = globalDataList.DataArray[dataIndex];
			updatedData.ItemScore += scoreChange;

			return Json(updatedData);
		}

		public IActionResult GetFullList()
		{
			// The return statement is for our JS.
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


		public IActionResult GetListSize()
		{
			return Json(globalDataList.DataArray.Length);
		}


		public IActionResult ResultsPage()
		{
			// This block of code is used to retrieve the lesser-voted for items, for populating the bottom of the results page. Not used in our JS.
			var remainingData = globalDataList.DataArray
				.OrderByDescending(data => data.ItemScore)
				.Skip(3)
				.ToList();
			ViewBag.RemainingData = remainingData;


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