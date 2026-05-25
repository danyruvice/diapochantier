using DiaspoChantier.Enums;
using DiaspoChantier.Models;
using DiaspoChantier.Services;
using DiaspoChantier.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DiaspoChantier.Controllers.Api;

[ApiController]
[Route("api/profil")]
[Authorize]
public class ProfilApiController : ControllerBase
{
    private readonly AuthService _auth;
    private readonly UserManager<Utilisateur> _users;
    private readonly ServiceStockage _stockage;

    public ProfilApiController(AuthService auth, UserManager<Utilisateur> users, ServiceStockage stockage)
    {
        _auth = auth;
        _users = users;
        _stockage = stockage;
    }

    [HttpGet]
    public async Task<IActionResult> GetProfil()
    {
        var utilisateur = await _users.GetUserAsync(User);
        if (utilisateur is null)
        {
            return Unauthorized();
        }

        utilisateur = await _auth.ObtenirUtilisateurActuelAsync(utilisateur.Id);
        if (utilisateur is null)
        {
            return NotFound();
        }

        return Ok(new
        {
            utilisateur.Id,
            utilisateur.Email,
            utilisateur.Prenom,
            utilisateur.Nom,
            utilisateur.Telephone,
            utilisateur.Adresse,
            utilisateur.PhotoProfilUrl,
            Role = utilisateur.Role.ToString(),
            Statut = utilisateur.Statut.ToString(),
            Specialite = utilisateur.ProfilEntrepreneur?.Specialite,
            Ville = utilisateur.ProfilEntrepreneur?.Ville,
            NumeroMoMo = utilisateur.ProfilEntrepreneur?.NumeroMoMo
        });
    }

    [HttpPut]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> PutProfil([FromForm] GestionProfilViewModel modele)
    {
        var utilisateur = await _users.GetUserAsync(User);
        if (utilisateur is null)
        {
            return Unauthorized();
        }

        utilisateur = await _auth.ObtenirUtilisateurActuelAsync(utilisateur.Id);
        if (utilisateur is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        string? urlPhoto = utilisateur.PhotoProfilUrl;
        if (modele.PhotoProfil is not null)
        {
            var upload = await _stockage.EnregistrerAsync(modele.PhotoProfil, "profils");
            urlPhoto = upload.Url;
        }

        var resultat = await _auth.MettreAJourProfilAsync(utilisateur, modele, urlPhoto);
        if (!resultat.Succeeded)
        {
            return BadRequest(new { errors = resultat.Errors.Select(e => e.Description) });
        }

        return Ok(new { message = "Profil mis à jour." });
    }
}
