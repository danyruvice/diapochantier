using DiaspoChantier.Services;
using DiaspoChantier.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiaspoChantier.Controllers.Api;

[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    private readonly AuthService _auth;

    public AuthApiController(AuthService auth)
    {
        _auth = auth;
    }

    [HttpPost("inscription-investisseur")]
    [AllowAnonymous]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> InscriptionInvestisseur([FromForm] InscriptionInvestisseurViewModel modele)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var resultat = await _auth.CreerCompteInvestisseurAsync(modele, connexionAutomatique: false);
        if (!resultat.Succeeded)
        {
            return BadRequest(new { errors = resultat.Errors.Select(e => e.Description) });
        }

        return Ok(new { message = "Compte investisseur créé. Email de confirmation envoyé.", statut = "AttenteVerification" });
    }

    [HttpPost("inscription-entrepreneur")]
    [AllowAnonymous]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> InscriptionEntrepreneur([FromForm] InscriptionEntrepreneurViewModel modele)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var resultat = await _auth.CreerCompteEntrepreneurAsync(modele);
        if (!resultat.Succeeded)
        {
            return BadRequest(new { errors = resultat.Errors.Select(e => e.Description) });
        }

        return Ok(new { message = "Dossier entrepreneur soumis. En attente de vérification admin.", statut = "AttenteVerification" });
    }

    [HttpPost("connexion")]
    [AllowAnonymous]
    public async Task<IActionResult> Connexion([FromBody] ConnexionViewModel modele)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var resultat = await _auth.ConnexionAsync(modele);
        if (!resultat.Succeeded)
        {
            return Unauthorized(new { message = resultat.MessageErreur });
        }

        var jetons = await _auth.GenererJetonConnexionAsync(modele.Email);
        if (jetons is null)
        {
            return Unauthorized(new { message = "Impossible de générer les jetons." });
        }

        return Ok(new
        {
            accessToken = jetons.AccessToken,
            refreshToken = jetons.RefreshToken,
            expireMinutes = jetons.ExpireMinutes
        });
    }

    [HttpPost("reset-password-request")]
    [AllowAnonymous]
    public async Task<IActionResult> DemandeReset([FromBody] DemandeResetPasswordViewModel modele)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        await _auth.DemanderReinitialisationMotDePasseAsync(modele.Email);
        return Ok(new { message = "Si cette adresse email est enregistrée, un lien de réinitialisation a été envoyé." });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordViewModel modele)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var resultat = await _auth.ReinitialiserMotDePasseAsync(modele);
        if (!resultat.Succeeded)
        {
            return BadRequest(new { errors = resultat.Errors.Select(e => e.Description) });
        }

        return Ok(new { message = "Mot de passe mis à jour." });
    }

    [HttpPost("rafraichir-jeton")]
    [AllowAnonymous]
    public async Task<IActionResult> Rafraichir([FromBody] RafraichirJetonViewModel modele)
    {
        var jetons = await _auth.RafraichirJetonAsync(modele.RefreshToken);
        if (jetons is null)
        {
            return Unauthorized(new { message = "Refresh token invalide ou expiré." });
        }

        return Ok(new
        {
            accessToken = jetons.AccessToken,
            refreshToken = jetons.RefreshToken,
            expireMinutes = jetons.ExpireMinutes
        });
    }
}
