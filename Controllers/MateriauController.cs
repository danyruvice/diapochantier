using DiaspoChantier.Data;
using DiaspoChantier.Enums;
using DiaspoChantier.Models;
using DiaspoChantier.Services;
using DiaspoChantier.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DiaspoChantier.Controllers;

[Authorize]
public class MateriauController : Controller
{
    private readonly ApplicationDbContext _contexte;
    private readonly UserManager<Utilisateur> _users;
    private readonly MateriauService _materiauService;

    public MateriauController(ApplicationDbContext contexte, UserManager<Utilisateur> users, MateriauService materiauService)
    {
        _contexte = contexte;
        _users = users;
        _materiauService = materiauService;
    }

    [HttpGet]
    [Authorize(Roles = nameof(RoleUtilisateur.Entrepreneur))]
    public async Task<IActionResult> Ajouter(int projetId)
    {
        var utilisateur = await _users.GetUserAsync(User);
        var entrepreneur = await _contexte.Entrepreneurs.FirstOrDefaultAsync(e => e.IdUtilisateur == utilisateur!.Id);
        if (entrepreneur is null)
        {
            return Forbid();
        }

        return View(new AjoutMateriauViewModel { IdProjet = projetId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = nameof(RoleUtilisateur.Entrepreneur))]
    public async Task<IActionResult> Ajouter(AjoutMateriauViewModel modele)
    {
        if (!ModelState.IsValid)
        {
            return View(modele);
        }

        var utilisateur = await _users.GetUserAsync(User);
        var entrepreneur = await _contexte.Entrepreneurs.FirstOrDefaultAsync(e => e.IdUtilisateur == utilisateur!.Id);
        if (entrepreneur is null)
        {
            return Forbid();
        }

        if (modele.Facture is null)
        {
            ModelState.AddModelError(nameof(modele.Facture), "La photo de facture est obligatoire.");
            return View(modele);
        }

        var materiau = await _materiauService.EnregistrerAchatAsync(
            modele.IdProjet,
            entrepreneur.IdEntrepreneur,
            modele.Fournisseur,
            modele.Designation,
            modele.Quantite,
            modele.Unite,
            modele.PrixTotal,
            modele.Facture,
            modele.HashFacture);

        TempData["MessageSucces"] = "Matériau enregistré. QR code généré pour vérification.";
        return RedirectToAction(nameof(Inventaire), new { projetId = modele.IdProjet });
    }

    [HttpGet]
    public async Task<IActionResult> Inventaire(int projetId)
    {
        var materiaux = await _contexte.Materiaux
            .Where(m => m.IdProjet == projetId)
            .OrderByDescending(m => m.DateCreation)
            .AsNoTracking()
            .ToListAsync();

        ViewBag.ProjetId = projetId;
        return View(materiaux);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Verifier(string code)
    {
        var (valide, materiau) = await _materiauService.VerifierQrAsync(code);
        ViewBag.Valide = valide;
        return View(materiau);
    }
}
