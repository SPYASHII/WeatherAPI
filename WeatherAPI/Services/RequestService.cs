using OpenAI_API;
using System.Text.Json;
using WeatherAPI.Constant;
using WeatherAPI.Models;

namespace WeatherAPI.Services
{
    public class RequestService
    {
        private static HttpClient _httpClient;
        public RequestService(HttpClient httpClient)
        {
            var socketsHandler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2)
            };
            _httpClient = new HttpClient(socketsHandler);
        }
        public async Task<CurrentWeatherData> GetCurrentWeatherDataAsync(string location)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, Constants.weatherApiUrl + "current.json?key=" + Constants.weatherApiKey + "&q=" + location + "&lang=uk");
            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                CurrentWeatherData responseContent = await JsonSerializer.DeserializeAsync<CurrentWeatherData>(response.Content.ReadAsStream());
                return responseContent;
            }

            return null;
        }
        public async Task<ForecastWeatherData> GetForecastWeatherDataAsync(string location, int days)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, Constants.weatherApiUrl + "forecast.json?key=" + Constants.weatherApiKey + "&q=" + location + "&days=" + days + "&lang=uk");
            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                ForecastWeatherData responseContent = await JsonSerializer.DeserializeAsync<ForecastWeatherData>(response.Content.ReadAsStream());
                return responseContent;
            }

            return null;
        }
        public async Task<ForecastWeatherData> GetDateWeatherDataAsync(string location, string date)
        {
            if (date.Count() != 10)
                throw new ArgumentException($"Wrong format of string date {date}. Must be date in format yyyy-mm-dd");
            var dateArr = date.Split('-', 3);
            int year = int.Parse(dateArr[0]);
            int month = int.Parse(dateArr[1]);
            int day = int.Parse(dateArr[2]);

            string time;
            try
            {
                time = "future";
                if (DateTime.Now.Date.CompareTo(new DateTime(year, month, day).Date) > 0)
                {
                    time = "history";
                }
            }
            catch (Exception e)
            {
                return null;
            }

            var request = new HttpRequestMessage(HttpMethod.Get, Constants.weatherApiUrl + time + ".json?key=" + Constants.weatherApiKey + "&q=" + location + "&dt=" + date + "&lang=uk");
            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                ForecastWeatherData responseContent = await JsonSerializer.DeserializeAsync<ForecastWeatherData>(response.Content.ReadAsStream());
                return responseContent;
            }

            var forecast = await GetForecastWeatherDataAsync(location, 14);
            if (forecast != null)
            {
                var _day = forecast.forecast.forecastday.FirstOrDefault(x => x.date == date);
                if (_day != null)
                {
                    forecast.forecast.forecastday.Clear();
                    forecast.forecast.forecastday.Add(_day);
                    return forecast;
                }
            }
            return null;
        }
        public async Task<string> GetChatGptAnswer(string location, string date, int c)
        {
            string condition = null;
            double[] parameters = new double[5] { 0, 0, 0, 0, 0 };
            if (c == 0)
            {
                ForecastWeatherData dayForecast = await GetDateWeatherDataAsync(location, date);
                if (dayForecast != null)
                {
                    var day = dayForecast.forecast.forecastday[0].day;
                    condition = day.condition.text;
                    parameters[0] = day.avgtemp_c;
                    parameters[1] = day.maxwind_kph;
                    parameters[2] = day.avghumidity;
                    parameters[3] = day.daily_chance_of_rain;
                    parameters[4] = day.daily_chance_of_snow;

                }
                else return null;
            }
            else
            {
                CurrentWeatherData current = await GetCurrentWeatherDataAsync(location);
                if (current != null)
                {
                    condition = current.current.condition.text;
                    parameters[0] = current.current.temp_c;
                    parameters[1] = current.current.wind_kph;
                    parameters[2] = current.current.humidity;
                    parameters[3] = current.current.precip_mm;
                    parameters[4] = current.current.gust_kph;
                }
                else return null;
            }
            if (condition != null)
            {

                OpenAIAPI api = new OpenAIAPI(Constants.openAiKey);
                var chat = api.Chat.CreateConversation();

                chat.AppendSystemMessage(Constants.chatGptPromt);
                if (c == 0)
                {
                    chat.AppendUserInput($"{condition}\nСередня температура протягом дня:{parameters[0]}°C\nШвидкість вітру:{parameters[1]}км/г\nВологість:{parameters[2]}%\nШанс того що буде дощ:{parameters[3]}%\nШанс того що буде сніг:{parameters[4]}%");
                }
                else
                {
                    chat.AppendUserInput($"{condition}\nТемпература:{parameters[0]}°C\nШвидкість вітру:{parameters[1]}км/ч\nВологість:{parameters[2]}%\nАтмосферні опади:{parameters[3]}мм\nПорив вітру:{parameters[4]}км/г");
                }
                return await chat.GetResponseFromChatbotAsync();
            }
            return null;
        }
        public async Task<bool> FindLocation(string location)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, Constants.weatherApiUrl + "search.json?key=" + Constants.weatherApiKey + "&q=" + location);
            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                List<Location> responseContent = await JsonSerializer.DeserializeAsync<List<Location>>(response.Content.ReadAsStream());

                if (responseContent.Count > 0)
                    return true;
                else
                    return false;
            }

            return false;
        }
    }
}
