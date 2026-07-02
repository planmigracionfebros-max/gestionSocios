using GestionSpa.Api.Data;
using GestionSpa.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionSpa.Api.Controllers;

[ApiController]
[Route("api/webhooks/portero")]
[AllowAnonymous]
public class PorteroWebhookController(
    AppDbContext db,
    IPorteroIntegrationService portero,
    ILogger<PorteroWebhookController> logger) : ControllerBase
{
    [HttpPost("{emisorSlug}")]
    public async Task<IActionResult> Recibir(string emisorSlug, [FromBody] PorteroWebhookPayload payload)
    {
        var emisor = await db.Emisores
            .Include(e => e.PorteroConfig)
            .FirstOrDefaultAsync(e => e.Slug == emisorSlug && e.Activo);

        if (emisor is null) return NotFound();

        var config = emisor.PorteroConfig;
        if (config is null || !config.Habilitado) return Ok();

        if (!string.IsNullOrEmpty(config.WebhookSecret))
        {
            var secret = Request.Headers["X-Webhook-Secret"].FirstOrDefault();
            if (!string.Equals(secret, config.WebhookSecret, StringComparison.Ordinal))
                return Unauthorized();
        }

        try
        {
            switch (payload.Event)
            {
                case "access":
                    await portero.ProcessAccessWebhookAsync(emisorSlug, payload);
                    break;
                default:
                    logger.LogInformation("Webhook portero {Event} recibido para {Slug}", payload.Event, emisorSlug);
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error procesando webhook portero {Event} para {Slug}", payload.Event, emisorSlug);
        }

        return Ok();
    }
}
