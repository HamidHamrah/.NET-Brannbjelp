using Ignist.Data;
using Ignist.Data.Services;
using System.Text;
using Microsoft.Azure.Cosmos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Add services to the container.

#region cosmosdb tilkobling
//Test


builder.Services.AddSingleton((provider) =>
{
    var configuration = provider.GetRequiredService<IConfiguration>(); 
    var EndpointUri = configuration["CosmosDbSettings:EndpointUri"];
    var PrimaryKey = configuration["CosmosDbSettings:PrimaryKey"];
    var databaseName = configuration["CosmosDbSettings:DatabaseName"];

    var cosmosClientOptions = new CosmosClientOptions
    {
        ApplicationName = databaseName,
        ConnectionMode = ConnectionMode.Gateway
    };
    var loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.AddConsole(); 
    });

    var cosmosClient = new CosmosClient(EndpointUri, PrimaryKey, cosmosClientOptions);

    return cosmosClient;

});
#endregion

builder.Services.AddControllers();

builder.Services.AddCors(publications => publications.AddPolicy("corspolicy", build =>
{
    build.WithOrigins("*").AllowAnyMethod().AllowAnyHeader();
}));



//JWT token Singleton
builder.Services.AddSingleton<JwtTokenService>(sp =>
    new JwtTokenService(sp.GetRequiredService<IConfiguration>()));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options=>
{
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Description = "Standard Autherization header using the Bearer scheme(\"bearer{token}\")",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });
    options.OperationFilter<SecurityRequirementsOperationFilter>();
});
#region Authetesering
builder.Services.AddScoped<ICosmosDbService, CosmosDbService>();
builder.Services.AddSingleton<PasswordHelper>();
#endregion

#region Legg til autentiseringstjenester
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"])),
            ValidateIssuer = true, // to validate the Issuer
            ValidateAudience = true, // to validate the Audience
            ValidIssuer = configuration["Jwt:Issuer"], // the Issuer from appsettings.json
            ValidAudience = configuration["Jwt:Audience"], // the Audience from appsettings.json
            ClockSkew = TimeSpan.Zero // Optional: reduces or eliminates clock skew tolerance
        };
    });

#endregion


#region Dette er publication tilkalling
builder.Services.AddScoped<IPublicationsRepository, PublicationsRepository>();
#endregion

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//This for Cors(Hamid)
app.UseCors("corspolicy"); //Using the Cors to in app

app.UseHttpsRedirection();

app.UseAuthentication(); // ny
app.UseAuthorization();

app.MapControllers();

app.Run();
