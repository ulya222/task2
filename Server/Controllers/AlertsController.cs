using Microsoft.AspNetCore.Mvc;

namespace DataVault.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AlertsController : ControllerBase
{
    [HttpPost("low-stock-notify")]
    public IActionResult SendLowStockNotify()
    {
        return Ok(new { sent = true, message = "Запрос на уведомление отправлен. Проверьте настройку Email в конфигурации." });
    }
}
