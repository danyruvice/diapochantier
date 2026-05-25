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
public class EntrepreneurController : Controller
{
    private readonly ApplicationDbContext _contexte;
    private readonly UserManager<Utilisateur> _gestionnaireUtilisateurs;
    private readonly ServiceEmail _serviceEmail;

    public EntrepreneurController(ApplicationDbContext contexte, UserManager<Utilisateur> gestionnaireUtilisateurs, ServiceEmail serviceEmail)
    {
        _contexte = contexte;
        _gestionnaireUtilisateurs = gestionnaireUtilisateurs;
        _serviceEmail = serviceEmail;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? recherche)
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

        var query = _contexte.Entrepreneurs
            .Include(e => e.Utilisateur)
            .Where(e => e.Utilisateur != null && e.Utilisateur.Statut == StatutCompte.Actif)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(recherche))
        {
            var terme = recherche.Trim();
            query = query.Where(e =>
                (e.Utilisateur != null && (e.Utilisateur.Prenom + " " + e.Utilisateur.Nom).Contains(terme))
                || e.Specialite.Contains(terme)
                || e.Ville.Contains(terme)
                || (!string.IsNullOrWhiteSpace(e.Description) && e.Description.Contains(terme)));
        }

        var entrepreneurs = await query
            .OrderByDescending(e => e.ScoreFilabilite)
            .ThenByDescending(e => e.NbProjetsTermines)
            .Take(50)
            .ToListAsync();

        var projets = await _contexte.Projets
            .Where(p => p.IdInvestisseur == utilisateur.Id)
            .OrderByDescending(p => p.DateCreation)
            .ToListAsync();

        var ids = entrepreneurs.Select(e => e.IdEntrepreneur).ToList();
        var avisParEntrepreneur = await _contexte.AvisEntrepreneurs
            .Where(a => ids.Contains(a.IdEntrepreneur))
            .GroupBy(a => a.IdEntrepreneur)
            .Select(g => new { Id = g.Key, Count = g.Count(), Moyenne = g.Average(a => a.Note) })
            .ToListAsync();

        var modele = new RechercheEntrepreneurViewModel
        {
            Recherche = recherche,
            Entrepreneurs = entrepreneurs.Select(e =>
            {
                var stats = avisParEntrepreneur.FirstOrDefault(a => a.Id == e.IdEntrepreneur);
                return MapEntrepreneurCarte(e, stats?.Count ?? 0, stats?.Moyenne ?? 0);
            }).ToList(),
            Projets = projets.Select(p => new ProjetSelectionViewModel
            {
                IdProjet = p.IdProjet,
                Titre = p.Titre,
                Statut = p.Statut.ToString()
            }).ToList()
        };

        return View(modele);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
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

        var entrepreneur = await _contexte.Entrepreneurs
            .Include(e => e.Utilisateur)
            .FirstOrDefaultAsync(e => e.IdEntrepreneur == id);

        if (entrepreneur is null || entrepreneur.Utilisateur?.Statut != StatutCompte.Actif)
        {
            return NotFound();
        }

        var avisStats = await _contexte.AvisEntrepreneurs
            .Where(a => a.IdEntrepreneur == id)
            .GroupBy(a => 1)
            .Select(g => new { Count = g.Count(), Moyenne = g.Average(a => a.Note) })
            .FirstOrDefaultAsync();

        var projets = await _contexte.Projets
            .Where(p => p.IdInvestisseur == utilisateur.Id)
            .OrderByDescending(p => p.DateCreation)
            .ToListAsync();

        var modele = new EntrepreneurProfilViewModel
        {
            IdEntrepreneur = entrepreneur.IdEntrepreneur,
            NomComplet = string.Join(" ", new[] { entrepreneur.Utilisateur?.Prenom, entrepreneur.Utilisateur?.Nom }.Where(x => !string.IsNullOrWhiteSpace(x))),
            Specialite = entrepreneur.Specialite,
            Ville = entrepreneur.Ville,
            Description = entrepreneur.Description,
            PhotoProfilUrl = entrepreneur.Utilisateur?.PhotoProfilUrl,
            ScoreFilabilite = entrepreneur.ScoreFilabilite,
            NbProjetsTermines = entrepreneur.NbProjetsTermines,
            NbAvis = avisStats?.Count ?? 0,
            NoteMoyenne = avisStats?.Moyenne ?? 0,
            NumeroMoMo = entrepreneur.NumeroMoMo,
            Projets = projets.Select(p => new ProjetSelectionViewModel
            {
                IdProjet = p.IdProjet,
                Titre = p.Titre,
                Statut = p.Statut.ToString()
            }).ToList()
        };

        return View(modele);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Inviter(int entrepreneurId, int projetId, string? message)
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

        var projet = await _contexte.Projets
            .FirstOrDefaultAsync(p => p.IdProjet == projetId && p.IdInvestisseur == utilisateur.Id);

        if (projet is null)
        {
            TempData["MessageErreur"] = "Le projet sélectionné est introuvable ou ne vous appartient pas.";
            return RedirectToAction(nameof(Index));
        }

        var entrepreneur = await _contexte.Entrepreneurs
            .Include(e => e.Utilisateur)
            .FirstOrDefaultAsync(e => e.IdEntrepreneur == entrepreneurId);

        if (entrepreneur is null || entrepreneur.Utilisateur is null || entrepreneur.Utilisateur.Statut != StatutCompte.Actif)
        {
            TempData["MessageErreur"] = "L’entrepreneur sélectionné est introuvable ou n’est pas encore vérifié.";
            return RedirectToAction(nameof(Index));
        }

        var invitationExistante = await _contexte.Invitations
            .FirstOrDefaultAsync(i => i.IdProjet == projet.IdProjet && i.IdEntrepreneur == entrepreneur.IdEntrepreneur);

        if (invitationExistante is not null)
        {
            TempData["MessageSucces"] = "Une invitation est déjà en attente pour cet entrepreneur.";
            return RedirectToAction(nameof(Details), new { id = entrepreneur.IdEntrepreneur });
        }

        var invitation = new InvitationProjet
        {
            IdProjet = projet.IdProjet,
            IdEntrepreneur = entrepreneur.IdEntrepreneur,
            Statut = StatutInvitation.EnAttente,
            Message = string.IsNullOrWhiteSpace(message) ? null : message.Trim(),
            DateCreation = DateTime.UtcNow
        };

        _contexte.Invitations.Add(invitation);
        await _contexte.SaveChangesAsync();

        try
        {
            await _serviceEmail.EnvoyerAsync(
                entrepreneur.Utilisateur.Email!,
                "Nouvelle invitation Diaspo-Chantier",
                $"<p>Bonjour {entrepreneur.Utilisateur.Prenom},</p><p>Vous avez reçu une nouvelle invitation pour le projet <strong>{projet.Titre}</strong>.</p><p>Connectez-vous à Diaspo-Chantier pour l’accepter ou la refuser.</p>");
        }
        catch
        {
            // Envoi non bloquant si la configuration SMTP n’est pas disponible.
        }

        TempData["MessageSucces"] = "Invitation envoyée à l’entrepreneur.";
        return RedirectToAction(nameof(Details), new { id = entrepreneur.IdEntrepreneur });
    }

    [HttpGet]
    public async Task<IActionResult> Invitations()
    {
        var utilisateur = await ObtenirUtilisateurActuelAsync();
        if (utilisateur is null)
        {
            return RedirectToAction("Connexion", "Auth");
        }

        if (utilisateur.Role != RoleUtilisateur.Entrepreneur)
        {
            return Forbid();
        }

        var entrepreneur = await _contexte.Entrepreneurs
            .FirstOrDefaultAsync(e => e.IdUtilisateur == utilisateur.Id);

        if (entrepreneur is null)
        {
            return Forbid();
        }

        var invitations = await _contexte.Invitations
            .Where(i => i.IdEntrepreneur == entrepreneur.IdEntrepreneur && i.Statut == StatutInvitation.EnAttente)
            .Include(i => i.Projet)
                .ThenInclude(p => p!.Investisseur)
            .OrderByDescending(i => i.DateCreation)
            .ToListAsync();

        return View(invitations);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Repondre(int invitationId, bool accepter, string? motifRefus)
    {
        var utilisateur = await ObtenirUtilisateurActuelAsync();
        if (utilisateur is null)
        {
            return RedirectToAction("Connexion", "Auth");
        }

        if (utilisateur.Role != RoleUtilisateur.Entrepreneur)
        {
            return Forbid();
        }

        var entrepreneur = await _contexte.Entrepreneurs
            .FirstOrDefaultAsync(e => e.IdUtilisateur == utilisateur.Id);

        if (entrepreneur is null)
        {
            return Forbid();
        }

        var invitation = await _contexte.Invitations
            .Include(i => i.Projet)
                .ThenInclude(p => p!.Investisseur)
            .FirstOrDefaultAsync(i => i.IdInvitation == invitationId && i.IdEntrepreneur == entrepreneur.IdEntrepreneur);

        if (invitation is null)
        {
            return NotFound();
        }

        if (!accepter && string.IsNullOrWhiteSpace(motifRefus))
        {
            TempData["MessageErreur"] = "Indiquez un motif pour refuser l’invitation.";
            return RedirectToAction(nameof(Invitations));
        }

        invitation.Statut = accepter ? StatutInvitation.Accepte : StatutInvitation.Refuse;
        invitation.DateReponse = DateTime.UtcNow;
        invitation.MotifRefus = accepter ? null : motifRefus?.Trim();

        if (accepter && invitation.Projet is not null)
        {
            invitation.Projet.IdEntrepreneur = entrepreneur.IdEntrepreneur;
            invitation.Projet.Statut = StatutProjet.EnCours;
            invitation.Projet.DateMiseAJour = DateTime.UtcNow;
        }

        await _contexte.SaveChangesAsync();

        if (!accepter && invitation.Projet?.Investisseur is not null && !string.IsNullOrWhiteSpace(invitation.Projet.Investisseur.Email))
        {
            try
            {
                await _serviceEmail.EnvoyerAsync(
                    invitation.Projet.Investisseur.Email,
                    "Invitation refusée — Diaspo-Chantier",
                    $"<p>Bonjour {invitation.Projet.Investisseur.Prenom},</p>" +
                    $"<p>L’entrepreneur a refusé votre invitation pour le projet <strong>{invitation.Projet.Titre}</strong>.</p>" +
                    $"<p>Motif : {invitation.MotifRefus}</p>");
            }
            catch
            {
                // Envoi non bloquant.
            }
        }

        TempData["MessageSucces"] = accepter ? "Invitation acceptée. Le projet est maintenant associé à votre profil." : "Invitation refusée. L’investisseur a été notifié.";

        return RedirectToAction(nameof(Invitations));
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

    private EntrepreneurCarteViewModel MapEntrepreneurCarte(Entrepreneur entrepreneur, int nbAvis, double noteMoyenne)
    {
        return new EntrepreneurCarteViewModel
        {
            IdEntrepreneur = entrepreneur.IdEntrepreneur,
            NomComplet = string.Join(" ", new[] { entrepreneur.Utilisateur?.Prenom, entrepreneur.Utilisateur?.Nom }.Where(x => !string.IsNullOrWhiteSpace(x))),
            Specialite = entrepreneur.Specialite,
            Ville = entrepreneur.Ville,
            Description = entrepreneur.Description,
            PhotoProfilUrl = entrepreneur.Utilisateur?.PhotoProfilUrl,
            ScoreFilabilite = entrepreneur.ScoreFilabilite,
            NbProjetsTermines = entrepreneur.NbProjetsTermines,
            NbAvis = nbAvis,
            NoteMoyenne = noteMoyenne,
            NumeroMoMo = entrepreneur.NumeroMoMo
        };
    }
}
