using MongoDB.Driver;
using WeatherAPI.Constant;
using WeatherAPI.Models;
using WeatherAPI.Services;
internal class Program
{
    private static void Main(string[] args)
    {
        var settings = MongoClientSettings.FromConnectionString(Constants.mongoDbUrl);
        settings.ServerApi = new ServerApi(ServerApiVersion.V1);
        Constants.mongoClient = new MongoClient(settings);
        Constants.usersDatabase = Constants.mongoClient.GetDatabase(Constants.mongoDbName);
        Constants.usersCollection = Constants.usersDatabase.GetCollection<UserData>("Users");

        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddHttpClient();
        builder.Services.AddScoped<RequestService>();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}