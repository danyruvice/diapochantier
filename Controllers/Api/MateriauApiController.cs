using DiaspoChantier.Data;
using DiaspoChantier.Enums;
using DiaspoChantier.Services;
using DiaspoChantier.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DiaspoChantier.Controllers.Api;

[ApiController]
[Route("api/materiaux")]
[Authorize(Roles = nameof(RoleUtilisateur.Entrepreneur))]
public class MateriauApiController : ControllerBase
{
    private readonly ApplicationDbContext _contexte;
    private readonly UserManager<Models.Utilisateur> _users;
    private readonly MateriauService _service;

    public MateriauApiController(ApplicationDbContext contexte, UserManager<Models.Utilisateur> users, MateriauService service)
    {
        _contexte = contexte;
        _users = users;
        _service = service;
    }

    [HttpPost]
    [RequestSizeLimit(15_000_000)]
    public async Task<IActionResult> Post([FromForm] AjoutMateriauViewModel modele)
    {
        if (!ModelState.IsValid || modele.Facture is null)
        {
            return BadRequest(ModelState);
        }

        var utilisateur = await _users.GetUserAsync(User);
        var entrepreneur = await _contexte.Entrepreneurs.FirstOrDefaultAsync(e => e.IdUtilisateur == utilisateur!.Id);
        if (entrepreneur is null)
        {
            return Forbid();
        }

        var materiau = await _service.EnregistrerAchatAsync(
            modele.IdProjet,
            entrepreneur.IdEntrepreneur,
            modele.Fournisseur,
            modele.Designation,
            modele.Quantite,
            modele.Unite,
            modele.PrixTotal,
            modele.Facture,
            modele.HashFacture);

        return Ok(new
        {
            materiau!.IdMateriau,
            materiau.HashSHA256,
            qrCode = materiau.QrCodePayload
        });
    }

    [HttpGet("verifier")]
    [AllowAnonymous]
    public async Task<IActionResult> Verifier([FromQuery] string code)
    {
        var (valide, materiau) = await _service.VerifierQrAsync(code);
        if (materiau is null)
        {
            return NotFound(new { valide = false });
        }

        return Ok(new { valide, materiau });
    }
}
