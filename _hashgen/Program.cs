
// Пароли задавайте только через аргументы или переменные окружения, не храните в коде.
if (args.Length == 0)
{
    Console.WriteLine("Использование: dotnet run -- \"пароль_для_хеширования\"");
    return;
}
var password = args[0];
Console.WriteLine("Хеш для введённого пароля: " + BCrypt.Net.BCrypt.HashPassword(password));
