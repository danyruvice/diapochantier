using DiaspoChantier.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiaspoChantier.Controllers.Api;

[ApiController]
[Route("webhooks")]
[AllowAnonymous]
public class WebhooksApiController : ControllerBase
{
    private readonly PaiementService _paiementService;
    private readonly ILogger<WebhooksApiController> _logger;

    public WebhooksApiController(PaiementService paiementService, ILogger<WebhooksApiController> logger)
    {
        _paiementService = paiementService;
        _logger = logger;
    }

    [HttpPost("stripe")]
    public async Task<IActionResult> Stripe()
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"].ToString();

        var resultat = await _paiementService.TraiterWebhookStripeAsync(payload, signature);
        if (!resultat.IsSuccess)
        {
            _logger.LogWarning("Webhook Stripe rejeté : {Message}", resultat.Message);
            return BadRequest(new { message = resultat.Message });
        }

        return Ok(new { received = true, dejaTraite = resultat.IsAlreadyProcessed });
    }

    [HttpPost("mtn-momo")]
    public async Task<IActionResult> MtnMomo()
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync();
        var secret = Request.Headers["X-MTN-Secret"].ToString();

        var resultat = await _paiementService.TraiterWebhookMtnAsync(payload, secret);
        if (!resultat.IsSuccess)
        {
            _logger.LogWarning("Webhook MTN rejeté : {Message}", resultat.Message);
            return BadRequest(new { message = resultat.Message });
        }

        return Ok(new { received = true, dejaTraite = resultat.IsAlreadyProcessed });
    }
}
