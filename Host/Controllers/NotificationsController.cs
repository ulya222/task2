using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;
using TelecomProd.Core;

namespace TelecomProd.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly TelecomDbContext _context;
    private readonly IConfiguration _config;

    public NotificationsController(TelecomDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    [HttpPost("low-stock-email")]
    public async Task<IActionResult> SendLowStockEmail([FromQuery] string? to)
    {
        var emailTo = to ?? _config["Email:To"];
        if (string.IsNullOrEmpty(emailTo)) return BadRequest(new { detail = "Укажите Email:To в appsettings или параметр to." });

        var lowStock = await _context.StockBalances
            .Include(sb => sb.Component)
            .Where(sb => sb.Quantity <= sb.Component.MinStock)
            .Select(sb => new { sb.Component!.Code, sb.Component.Name, sb.Quantity, sb.Component.MinStock })
            .Take(50).ToListAsync();

        var host = _config["Email:SmtpHost"];
        var port = _config.GetValue<int>("Email:SmtpPort", 587);
        var user = _config["Email:User"];
        var password = _config["Email:Password"];
        if (string.IsNullOrEmpty(host))
            return Ok(new { sent = false, message = "Email не настроен (SmtpHost). Сообщение не отправлено.", lowStockCount = lowStock.Count });

        try
        {
            using var client = new SmtpClient(host, port)
            {
                EnableSsl = port == 465 || port == 587,
                Credentials = string.IsNullOrEmpty(user) ? null : new NetworkCredential(user, password)
            };
            var body = "Список позиций с низким остатком:\n\n" +
                string.Join("\n", lowStock.Select(x => $"{x.Code} {x.Name}: остаток {x.Quantity}, мин. {x.MinStock}"));
            var msg = new MailMessage(
                _config["Email:From"] ?? "noreply@telecomprod.local",
                emailTo,
                "TelecomProd: низкие остатки",
                body);
            await client.SendMailAsync(msg);
            return Ok(new { sent = true, lowStockCount = lowStock.Count });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { sent = false, error = ex.Message });
        }
    }
}
