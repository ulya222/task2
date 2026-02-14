using Microsoft.EntityFrameworkCore;
using TelecomProd.Core;
using System.Text.Json.Serialization;
using Npgsql;

if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")))
    Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

if (args.Length > 0 && args[0] == "hash")
{
    var pwd = Environment.GetEnvironmentVariable("TELECOM_HASH_PASSWORD");
    if (string.IsNullOrEmpty(pwd)) { Console.WriteLine("Задайте TELECOM_HASH_PASSWORD для генерации хеша."); return; }
    Console.WriteLine("Hash: " + BCrypt.Net.BCrypt.HashPassword(pwd));
    return;
}

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://127.0.0.1:5000");

var connStr = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
var password = Environment.GetEnvironmentVariable("TELECOM_DB_PASSWORD") ?? builder.Configuration["Postgres:Password"];
if (!string.IsNullOrEmpty(password))
{
    var csb = new NpgsqlConnectionStringBuilder(connStr) { Password = password };
    connStr = csb.ConnectionString;
}

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<TelecomDbContext>(options => options.UseNpgsql(connStr));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowClient");
app.UseAuthorization();
app.MapControllers();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TelecomDbContext>();
    try
    {
        await db.Database.CanConnectAsync();
        logger.LogInformation("[TelecomProd.Host] БД telecomprod_db подключена.");
        Console.WriteLine(">>> [TelecomProd.Host] БД подключена (PostgreSQL). <<<");
    }
    catch (Exception ex)
    {
        var msg = ex.InnerException?.Message ?? ex.Message;
        logger.LogError("[TelecomProd.Host] Ошибка БД: {Message}", msg);
        Console.WriteLine(">>> Ошибка БД: " + msg + " <<<");
    }
}

app.Run();
