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

		public string ApiKey { get; }

		public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
		{
			_logger = logger;
			_httpClient = new HttpClient();
			_configuration = configuration;

            // Read the API key from configuration
            ApiKey = _configuration.GetSection("ApiSettings")["ApiKey"];
        }


    // Global Datasets

		// Barnames stored locally for testing (and practical use) when APIs are failing.
		string[] barNames = {
			"TomCat",
			"Greaser",
			"CrowBar",
			"Wooly Mammoth",
			"Rics Backyard",

			"BlackBear",
			"Marquee Bar",
			"Empire Hotel",
			"Mr Percivals",
			"Felons",

			"Death and Taxes",
			"Bar Pacino",
			"Mr Mista",
			"Laruche",
			"Oche",

			"Sasquatch Bar",
			"Bar Brutus",
			"The Bearded Lady",
			"Archive",
			"Brewdog",

			"Netherworld",
			"Frog's Hollow Saloon",
			"The Pav Bar",
			"Miss Demeanour",
			"Embassy Hotel",

			"The Gresham Bar",
			"Blackbird",
			"Fat Louies",
			"Southbank Beer Garden",
			"The Plough Inn"
		};

		string[] masculineNames = { };

		string[] feminineNames = { };

		string[] neutralNames = { };



        public IActionResult Index()
		{
			return View();
		}

		public async Task<string> GetRandomDataAsync(string topic, int dataSize)
		{
			string[] dataType = { };
			switch (topic)
			{
				case "1":
					dataType = barNames;
					break;

				case "2":
					await GetNames("masculine");
					dataType = masculineNames;
					break;

				case "3":
					await GetNames("feminine");
					dataType = feminineNames;
					break;

                case "4":
                    await GetNames("neutral");
                    dataType = neutralNames;
                    break;
            }


			// Perform a Fisher-Yates shuffle on the array
			Random random = new Random();

            for (int i = dataType.Length - 1; i > 0; i--)
            {
                int j = random.Next(0, i + 1);
                string temp = dataType[i];
                dataType[i] = dataType[j];
                dataType[j] = temp;
            }


            // Take the first 'dataSize' elements to create a new list
            List<string> selectedData = dataType.Take(dataSize).ToList();

            // Serialize the list to JSON
            string json = JsonSerializer.Serialize(selectedData);


            return json;
		}

		public async Task<IActionResult> GetNames(string type)
		{
			// Used for removing duplicate names later on
            HashSet<string> uniqueNames = new HashSet<string>();

			// setting up our http client and including our API key in the request header
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);



            for (int i = 0; i < 4; i++)
            {
				string endpoint = "";

				switch (type)
				{
					case "masculine":
						endpoint = "https://api.api-ninjas.com/v1/babynames?gender=boy";
						break;

					case "feminine":
                        endpoint = "https://api.api-ninjas.com/v1/babynames?gender=girl";
						break;

					case "neutral":
                        endpoint = "https://api.api-ninjas.com/v1/babynames?gender=neutral";
                        break;
                }

                var response = await httpClient.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();

                if (!response.IsSuccessStatusCode)
                {
                    return BadRequest("API data retrieval failed");
                }

                // Read the JSON data from the response
                var jsonData = await response.Content.ReadAsStringAsync();

                // Deserialize the JSON data into a string array
                var namesFromApi = JsonSerializer.Deserialize<string[]>(jsonData);

                // Add the names to the HashSet to remove duplicates
                foreach (string name in namesFromApi)
                {
                    uniqueNames.Add(name);
                }
            }

            // Convert the HashSet to an array of strings with a maximum of 40 names
            string[] uniqueNamesArray = uniqueNames.Take(30).ToArray();


            switch (type)
            {
                case "masculine":
					masculineNames = uniqueNamesArray;
                    break;

                case "feminine":
					feminineNames = uniqueNamesArray;
					break;

                case "neutral":
					neutralNames = uniqueNamesArray;
                    break;
            }

            return null;
        }

		public async Task<IActionResult> SecondPage(string topic, int itemCount, int listSize)
		{
			string appData = null;



			// Switch statement here is used to hit the correct API based on user input and obtain data for the rest of the program.
			switch (topic)
			{
				case "1":
					appData = await GetRandomDataAsync("1", listSize);
					break;

				case "2":

					appData = await GetRandomDataAsync("2", listSize);
					break;

				case "3":
					appData = await GetRandomDataAsync("3", listSize);
					break;

				case "4":
					appData = await GetRandomDataAsync("4", listSize);
					break;

				case "5":
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