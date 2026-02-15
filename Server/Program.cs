using Microsoft.EntityFrameworkCore;
using DataVault.Core;
using DataVault.Core.Entities;
using System.Text.Json.Serialization;

if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")))
    Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

if (args.Length > 0 && args[0] == "hash")
{
    var pwd = Environment.GetEnvironmentVariable("DATAVAULT_HASH_PASSWORD");
    if (string.IsNullOrEmpty(pwd)) { Console.WriteLine("Задайте DATAVAULT_HASH_PASSWORD для генерации хеша."); return; }
    Console.WriteLine("Hash: " + BCrypt.Net.BCrypt.HashPassword(pwd));
    return;
}

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://127.0.0.1:5050");

var connStr = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Server=(localdb)\\mssqllocaldb;Database=DataVaultDb;Trusted_Connection=True;";
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<DataVaultDbContext>(options => options.UseSqlServer(connStr));

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
    var db = scope.ServiceProvider.GetRequiredService<DataVaultDbContext>();
    try
    {
        await db.Database.EnsureCreatedAsync();
        await db.Database.CanConnectAsync();
        if (!await db.Roles.AnyAsync())
        {
            await db.Database.ExecuteSqlRawAsync(
                "SET IDENTITY_INSERT [AppRole] ON; " +
                "INSERT INTO [AppRole] ([Id], [Name]) VALUES (1, N'Администратор'), (2, N'Руководитель'), (3, N'Специалист'), (4, N'Кладовщик'); " +
                "SET IDENTITY_INSERT [AppRole] OFF;");
        }
        if (!await db.TaskPhases.AnyAsync())
        {
            await db.Database.ExecuteSqlRawAsync(
                "SET IDENTITY_INSERT [TaskPhase] ON; " +
                "INSERT INTO [TaskPhase] ([Id], [Name]) VALUES (1, N'Новый'), (2, N'В работе'), (3, N'На проверке'), (4, N'Приостановлен'), (5, N'На доработке'), (6, N'Завершён'); " +
                "SET IDENTITY_INSERT [TaskPhase] OFF;");
        }
        if (!await db.Users.AnyAsync())
        {
            db.Users.Add(new AppUser
            {
                Login = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
                FullName = "Администратор",
                RoleId = 1
            });
            await db.SaveChangesAsync();
        }
        if (!await db.Categories.AnyAsync())
        {
            db.Categories.Add(new Category { Code = "CAT-001", Name = "Основная категория", Description = "Пример" });
            await db.SaveChangesAsync();
        }
        if (!await db.Storages.AnyAsync())
        {
            db.Storages.Add(new Storage { Name = "Основное хранилище", StorageKind = "main", Capacity = 10000 });
            await db.SaveChangesAsync();
        }
        logger.LogInformation("[DataVault.Server] БД подключена (SQL Server).");
        Console.WriteLine(">>> [DataVault.Server] БД подключена (SQL Server). <<<");
    }
    catch (Exception ex)
    {
        var msg = ex.InnerException?.Message ?? ex.Message;
        logger.LogError("[DataVault.Server] Ошибка БД: {Message}", msg);
        Console.WriteLine(">>> Ошибка БД: " + msg + " <<<");
    }
}

app.Run();
