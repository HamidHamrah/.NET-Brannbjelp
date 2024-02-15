using Ignist.Data;
using Microsoft.Azure.Cosmos;


var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Add services to the container.

#region MY Code


builder.Services.AddSingleton((provider) =>
{
    var configuration = provider.GetRequiredService<IConfiguration>(); // Hent konfigurasjonen gjennom provider
    var EndpointUri = configuration["CosmosDbSettings:EndpointUri"];
    var PrimaryKey = configuration["CosmosDbSettings:PrimaryKey"];
    var databaseName = configuration["CosmosDbSettings:DatabaseName"];

    var cosmosClientOptions = new CosmosClientOptions
    {
        ApplicationName = databaseName,
        ConnectionMode = ConnectionMode.Gateway // Sett ConnectionMode her
    };
    var loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.AddConsole(); // Legg til manglende semikolon her
    });

    var cosmosClient = new CosmosClient(EndpointUri, PrimaryKey, cosmosClientOptions);

    return cosmosClient;

});
#endregion

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

#region My Code
builder.Services.AddScoped<IPublicationsRepository, PublicationsRepository>();
#endregion

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
