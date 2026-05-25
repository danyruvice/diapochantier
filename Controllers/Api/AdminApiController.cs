using DiaspoChantier.Enums;
using DiaspoChantier.Services;
using DiaspoChantier.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiaspoChantier.Controllers.Api;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = nameof(RoleUtilisateur.Administrateur))]
public class AdminApiController : ControllerBase
{
    private readonly AuthService _auth;

    public AdminApiController(AuthService auth)
    {
        _auth = auth;
    }

    [HttpGet("verifications")]
    public async Task<IActionResult> ListeVerifications()
    {
        var dossiers = await _auth.RecupererEntrepreneursEnAttenteAsync();
        return Ok(dossiers);
    }

    [HttpGet("verifications/{id:int}")]
    public async Task<IActionResult> DetailVerification(int id)
    {
        var dossier = await _auth.RecupererDossierVerificationAsync(id);
        if (dossier is null)
        {
            return NotFound();
        }

        return Ok(dossier);
    }

    [HttpPatch("verifications/{id:int}/statut")]
    public async Task<IActionResult> ChangerStatut(int id, [FromBody] DecisionVerificationViewModel modele)
    {
        modele.IdEntrepreneur = id;

        if (!string.Equals(modele.Decision, "Approuver", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(modele.Decision, "Rejeter", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "La décision doit être Approuver ou Rejeter." });
        }

        if (string.Equals(modele.Decision, "Rejeter", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(modele.MotifRejet))
        {
            return BadRequest(new { message = "Le motif du rejet est obligatoire." });
        }

        var statut = string.Equals(modele.Decision, "Approuver", StringComparison.OrdinalIgnoreCase)
            ? StatutCompte.Actif
            : StatutCompte.Rejete;

        var succes = await _auth.ChangerStatutVerificationAsync(id, statut, modele.MotifRejet);
        if (!succes)
        {
            return NotFound();
        }

        return Ok(new { message = "Statut mis à jour.", statut = statut.ToString() });
    }
}
