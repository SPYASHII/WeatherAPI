using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Text.Json;
using WeatherAPI.Constant;
using WeatherAPI.Models;
using WeatherAPI.Services;

namespace WeatherAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherApiController : ControllerBase
    {
        private RequestService _requestService;
        public WeatherApiController(RequestService requestService)
        {
            _requestService = requestService;
        }

        [HttpGet("weather/current/{location}")]
        public async Task<IActionResult> GetCurrentWeather(string location)
        {
            CurrentWeatherData responseContent = await _requestService.GetCurrentWeatherDataAsync(location);

            if (responseContent != null)
            {
                return Ok(responseContent);
            }

            return NotFound();
        }

        [HttpGet("weather/forecast/{location}/{days}")]
        public async Task<IActionResult> GetForecast(string location, int days)
        {
            ForecastWeatherData responseContent = await _requestService.GetForecastWeatherDataAsync(location, days);

            if (responseContent != null)
            {
                return Ok(responseContent);
            }

            return NotFound();
        }

        [HttpGet("weather/date/{location}/{date}")]
        public async Task<IActionResult> GetWeatherByDate(string location, string date)
        {
            ForecastWeatherData responseContent = await _requestService.GetDateWeatherDataAsync(location, date);

            if (responseContent != null)
            {
                return Ok(responseContent);
            }

            return NotFound();
        }

        [HttpGet("weather/find/{location}/{weather}")]
        public async Task<IActionResult> GetDayByWeather(string location, string weather, int days = 14)
        {
            ForecastWeatherData responseContent = await _requestService.GetForecastWeatherDataAsync(location, days);
            if (responseContent != null)
            {
                var day = FindDayByWeather(responseContent, weather);
                if (day != null)
                {
                    responseContent.forecast.forecastday.Clear();
                    responseContent.forecast.forecastday.Add(day);

                    return Ok(responseContent);
                }
                return NoContent();
            }
            return NotFound();
        }
        private static Forecastday FindDayByWeather(ForecastWeatherData forecast, string weather)
        {
            var day = forecast.forecast.forecastday.FirstOrDefault(x => x.day.condition.text.Equals(weather) || x.day.condition.text.Contains(weather) || x.day.condition.text.Contains(weather.ToLower()));
            return day;
        }

        [HttpGet("weather/gpt/{location}")]
        public async Task<IActionResult> GetGptAnswer(string location, [FromQuery] string date = null, [FromQuery] int c = 0)
        {
            try
            {
                if (date == null && c == 0)
                    return NoContent();

                string responseContent = await _requestService.GetChatGptAnswer(location, date, c);

                if (responseContent != null)
                {
                    return Ok(responseContent);
                }

                return NotFound();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("users/{chatId}")]
        public async Task<IActionResult> GetUser(long chatId)
        {
            try
            {
                var filter = Builders<UserData>.Filter.Eq(user => user.ChatId, chatId);
                var user = await Constants.usersCollection.Find(filter).FirstOrDefaultAsync();
                if (user != null)
                    return Ok(user);
                else
                    return NotFound();

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("users/notify")]
        public async Task<IActionResult> GetUsersToNotify()
        {
            try
            {
                var filter = Builders<UserData>.Filter.Eq(user => user.notification.notify, true);

                var users = await Constants.usersCollection.Find(filter).ToListAsync<UserData>();
                if (users != null)
                    return Ok(users);
                else
                    return NotFound();

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("users/create")]
        public async Task<IActionResult> PostUser(long id)
        {
            try
            {
                TimeZoneInfo gmtPlus3TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Greenwich Standard Time");
                UserData newUser = new()
                {
                    ChatId = id,
                    notification = new Notification()
                    {
                        notificationTime = TimeZoneInfo.ConvertTime(DateTime.Now, gmtPlus3TimeZone).AddHours(6),
                        days = 0,
                    },
                };
                await Constants.usersCollection.InsertOneAsync(newUser);

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPatch("users/{chatId}")]
        public async Task<IActionResult> PatchUser(string? jsonNotification, long chatId, string? loc, bool? waitL, bool? f, bool? wd, bool? dw, bool? gptc, bool? gptd, bool? waitT, bool? waitD, bool? waitW)
        {
            try
            {
                var filter = Builders<UserData>.Filter.Eq(user => user.ChatId, chatId);
                UpdateDefinitionBuilder<UserData> builder = Builders<UserData>.Update;
                List<UpdateDefinition<UserData>> updates = new List<UpdateDefinition<UserData>>();

                if (loc != null)
                    updates.Add(Builders<UserData>.Update.Set(user => user.location, loc));

                if (waitL != null)
                    updates.Add(Builders<UserData>.Update.Set(user => user.waitingForLocation, waitL));

                if (f != null)
                    updates.Add(Builders<UserData>.Update.Set(user => user.foreCast, f));

                if (wd != null)
                    updates.Add(Builders<UserData>.Update.Set(user => user.weatherByDate, wd));

                if (dw != null)
                    updates.Add(Builders<UserData>.Update.Set(user => user.dayByWeather, dw));

                if (gptc != null)
                    updates.Add(Builders<UserData>.Update.Set(user => user.gptCurrent, gptc));

                if (gptd != null)
                    updates.Add(Builders<UserData>.Update.Set(user => user.gptByDate, gptd));

                if (waitD != null)
                    updates.Add(Builders<UserData>.Update.Set(user => user.waitingForDays, waitD));

                if (waitT != null)
                    updates.Add(Builders<UserData>.Update.Set(user => user.waitingForTime, waitT));

                if (waitW != null)
                    updates.Add(Builders<UserData>.Update.Set(user => user.waitingForWeather, waitW));

                if (jsonNotification != null)
                {
                    Notification notification = JsonSerializer.Deserialize<Notification>(jsonNotification);
                    updates.Add(Builders<UserData>.Update.Set(user => user.notification, notification));
                }

                if (updates.Count > 0)
                {
                    var update = builder.Combine(updates);

                    await Constants.usersCollection.UpdateOneAsync(filter, update);

                    return Ok();
                }
                else return BadRequest("Null parametrs");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("location/find/{location}")]
        public async Task<IActionResult> GetLocation(string location)
        {
            if (await _requestService.FindLocation(location))
            {
                return Ok();
            }
            return NotFound();
        }
    }
}