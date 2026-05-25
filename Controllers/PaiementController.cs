using System.Globalization;
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
public class PaiementController : Controller
{
    private readonly ApplicationDbContext _contexte;
    private readonly UserManager<Utilisateur> _gestionnaireUtilisateurs;
    private readonly PaiementService _paiementService;

    public PaiementController(ApplicationDbContext contexte, UserManager<Utilisateur> gestionnaireUtilisateurs, PaiementService paiementService)
    {
        _contexte = contexte;
        _gestionnaireUtilisateurs = gestionnaireUtilisateurs;
        _paiementService = paiementService;
    }

    [HttpGet]
    public async Task<IActionResult> Depot()
    {
        var utilisateur = await ObtenirUtilisateurActuelAsync();
        if (utilisateur is null)
        {
            return RedirectToAction("Connexion", "Auth");
        }

        if (utilisateur.Role != RoleUtilisateur.Investisseur)
        {
            return Forbid();
        }

        var projets = await _contexte.Projets
            .Where(p => p.IdInvestisseur == utilisateur.Id)
            .OrderByDescending(p => p.DateCreation)
            .AsNoTracking()
            .Select(p => new ElementProjetViewModel
            {
                IdProjet = p.IdProjet,
                Titre = p.Titre,
                TypeProjet = p.TypeProjet,
                Localisation = p.Localisation,
                BudgetTotal = p.BudgetTotal,
                Statut = p.Statut.ToString(),
                DateCreation = p.DateCreation,
                JalonsTotal = p.Jalons.Count,
                TotalJalons = p.Jalons.Sum(j => j.Montant),
                JalonsValides = p.Jalons.Count(j => j.EstValide),
                JalonsEnAttente = p.Jalons.Count(j => !j.EstValide)
            })
            .ToListAsync();

        var sequestres = await _contexte.Sequestres
            .Where(s => s.IdInvestisseur == utilisateur.Id)
            .AsNoTracking()
            .ToListAsync();

        var vue = new DepotPaiementViewModel
        {
            Projets = projets,
            SoldeBloque = sequestres.Sum(s => s.MontantBloque),
            SoldeLibere = sequestres.Sum(s => s.MontantLibere),
            MessageSucces = TempData["MessageSucces"] as string,
            MessageErreur = TempData["MessageErreur"] as string
        };

        return View(vue);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Initier(int projetId, decimal montant, string canal, string devise, string? telephone)
    {
        var utilisateur = await ObtenirUtilisateurActuelAsync();
        if (utilisateur is null)
        {
            return RedirectToAction("Connexion", "Auth");
        }

        if (utilisateur.Role != RoleUtilisateur.Investisseur)
        {
            return Forbid();
        }

        var resultat = await _paiementService.InitierDepotAsync(utilisateur.Id, projetId, montant, canal, devise, telephone);
        if (!resultat.IsSuccess)
        {
            return Json(new { success = false, message = resultat.Message });
        }

        return Json(new
        {
            success = true,
            message = resultat.Message,
            clientSecret = resultat.ClientSecret,
            paymentIntentId = resultat.PaymentIntentId,
            referenceExterne = resultat.ReferenceExterne,
            paiementId = resultat.PaiementId
        });
    }

    [HttpGet]
    public async Task<IActionResult> Historique()
    {
        var utilisateur = await ObtenirUtilisateurActuelAsync();
        if (utilisateur is null)
        {
            return RedirectToAction("Connexion", "Auth");
        }

        var paiements = await _contexte.Paiements
            .Where(p => p.IdInvestisseur == utilisateur.Id || (p.IdEntrepreneur.HasValue && p.Entrepreneur != null && p.Entrepreneur.IdUtilisateur == utilisateur.Id))
            .Include(p => p.Projet)
            .OrderByDescending(p => p.DateCreation)
            .AsNoTracking()
            .ToListAsync();

        var sequestres = await _contexte.Sequestres
            .Where(s => s.IdInvestisseur == utilisateur.Id)
            .AsNoTracking()
            .ToListAsync();

        var vue = new HistoriquePaiementViewModel
        {
            SoldeBloque = sequestres.Sum(s => s.MontantBloque),
            SoldeLibere = sequestres.Sum(s => s.MontantLibere),
            Paiements = paiements.Select(p => new DetailPaiementViewModel
            {
                IdPaiement = p.IdPaiement,
                TitreProjet = p.Projet?.Titre ?? "Projet non identifié",
                Montant = p.Montant,
                Canal = p.Canal.ToString(),
                TypePaiement = p.TypePaiement.ToString(),
                Statut = p.Statut.ToString(),
                DateCreation = p.DateCreation,
                Message = p.Message ?? string.Empty
            }).ToList()
        };

        return View(vue);
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> WebhookStripe([FromBody] string payload)
    {
        var signature = Request.Headers["Stripe-Signature"].ToString();
        var resultat = await _paiementService.TraiterWebhookStripeAsync(payload, signature);
        if (!resultat.IsSuccess)
        {
            return BadRequest(resultat.Message);
        }

        return Ok();
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> WebhookMtn([FromBody] string payload)
    {
        var secret = Request.Headers["X-MTN-Secret"].ToString();
        var resultat = await _paiementService.TraiterWebhookMtnAsync(payload, secret);
        if (!resultat.IsSuccess)
        {
            return BadRequest(resultat.Message);
        }

        return Ok();
    }

    private async Task<Utilisateur?> ObtenirUtilisateurActuelAsync()
    {
        var identifiant = _gestionnaireUtilisateurs.GetUserId(User);
        if (string.IsNullOrWhiteSpace(identifiant))
        {
            return null;
        }

        return await _gestionnaireUtilisateurs.FindByIdAsync(identifiant);
    }
}
