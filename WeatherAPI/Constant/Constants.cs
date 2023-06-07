using MongoDB.Driver;
using WeatherAPI.Models;

namespace WeatherAPI.Constant
{
    public class Constants
    {
        public static IMongoCollection<UserData> usersCollection;
        public static IMongoDatabase usersDatabase;
        public static MongoClient mongoClient;

        public const string weatherApiUrl = "http://api.weatherapi.com/v1/";
        public const string weatherApiKey = "7a6ab88a2abc4adcac4170025230206";

        private const string mongoDbUserName = "SPY";
        private const string mongoDbKey = "60ljxvBVBQIBC8og";

        public const string mongoDbName = "WeatherHelper";
        public const string mongoDbUrl = $"mongodb+srv://{mongoDbUserName}:{mongoDbKey}@weatherapi.glujpus.mongodb.net/?retryWrites=true&w=majority";

        public const string openAiKey = "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";
        public const string openAiUrl = "https://api.openai.com/v1/chat/completions";
        public const string chatGptPromt = "Ти допомічник з питань як вдягатися у певних погодніх умовах. User буде надсилати тобі повідомлення з данними про погоду.\r\nТи повинен відповісти на повідомлення коротко і чітко.\r\nПорекомендувати як краще вдягтись, враховуючі усі погодні фактори які тобі повідомили.\r\nУ відповіді не можна називати данні які тобі надіслав User";
    }
}
