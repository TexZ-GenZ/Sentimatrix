using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using SentimatrixAPI.Services;
using SentimatrixAPI.Data;
using Microsoft.OpenApi.Models;
using SentimatrixAPI.Hubs;
using MongoDB.Driver;
using Microsoft.Extensions.Options;
using System.Net;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Yarp.ReverseProxy.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

// Register GroqService
builder.Services.AddSingleton<GroqService>();

// Add MongoDB Settings
builder.Services.Configure<MongoDBSettings>(
    builder.Configuration.GetSection("MongoDBSettings"));

// In your service configuration section, replace the existing EmailService registration with:
builder.Services.AddSingleton<EmailService>(sp => 
{
    var settings = sp.GetRequiredService<IOptions<MongoDBSettings>>();
    var logger = sp.GetRequiredService<ILogger<EmailService>>();
    var cache = sp.GetRequiredService<IDistributedCache>();
    return new EmailService(settings, logger, cache);
});

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetValue<string>("Redis:ConnectionString", "localhost:6379");
    options.InstanceName = builder.Configuration.GetValue<string>("Redis:InstanceName", "SentimatrixCache_");
});

builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(
    builder.Configuration.GetValue<string>("Redis:ConnectionString", "localhost:6379")
));

// Register MongoClient and IMongoDatabase
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoDBSettings>>().Value;
    return new MongoClient(settings.ConnectionString);
});

builder.Services.AddScoped<IMongoDatabase>(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var settings = sp.GetRequiredService<IOptions<MongoDBSettings>>().Value;
    return client.GetDatabase(settings.DatabaseName);
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    // Option 1: Bind to multiple ports
    serverOptions.Listen(IPAddress.Any, 5000); // First instance
    serverOptions.Listen(IPAddress.Any, 5001); // Second instance
    serverOptions.Listen(IPAddress.Any, 5002); // Third instance

    // Optional: Configure HTTPS if needed
    // serverOptions.Listen(IPAddress.Any, 5443, listenOptions =>
    // {
    //     listenOptions.UseHttps(httpsOptions =>
    //     {
    //         httpsOptions.ServerCertificate = new X509Certificate2("path/to/certificate.pfx", "password");
    //     });
    // });
});

// Add YARP Reverse Proxy configuration
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();



// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c => 
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sentimatrix API v1");
});

app.UseRouting();
app.UseCors();
app.UseAuthorization();

app.MapControllers();
app.MapHub<TicketHub>("/ticketHub");

// Map the reverse proxy
app.MapReverseProxy();

app.Run();
