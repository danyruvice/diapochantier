using DiaspoChantier.Data;
using DiaspoChantier.Enums;
using DiaspoChantier.Models;
using DiaspoChantier.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DiaspoChantier.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _contexte;
        private readonly UserManager<Utilisateur> _userManager;

        public DashboardController(ApplicationDbContext contexte, UserManager<Utilisateur> userManager)
        {
            _contexte = contexte;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var utilisateur = await _userManager.GetUserAsync(User);
            if (utilisateur is null)
            {
                return RedirectToAction("Connexion", "Auth");
            }

            if (utilisateur.Role == RoleUtilisateur.Administrateur)
            {
                return RedirectToAction("Index", "Admin");
            }

            if (utilisateur.Role == RoleUtilisateur.Investisseur)
            {
                var projets = await _contexte.Projets
                    .Where(p => p.IdInvestisseur == utilisateur.Id)
                    .Include(p => p.Jalons)
                    .Include(p => p.Entrepreneur)
                        .ThenInclude(e => e!.Utilisateur)
                    .OrderByDescending(p => p.DateCreation)
                    .ToListAsync();

                var invitations = await _contexte.Invitations
                    .Where(i => i.Projet != null && i.Projet.IdInvestisseur == utilisateur.Id)
                    .Include(i => i.Projet)
                    .Include(i => i.Entrepreneur)
                        .ThenInclude(e => e!.Utilisateur)
                    .OrderByDescending(i => i.DateCreation)
                    .Take(5)
                    .ToListAsync();

                var activeProjects = projets.Where(p => p.Statut is not StatutProjet.Termine and not StatutProjet.Annule).ToList();
                var pendingMilestones = projets.Sum(p => p.Jalons.Count(j => !j.EstValide));
                var entrepreneursActifs = await _contexte.Entrepreneurs.CountAsync(e => e.Utilisateur != null && e.Utilisateur.Statut == StatutCompte.Actif);
                var sequestres = await _contexte.Sequestres
                    .Where(s => s.IdInvestisseur == utilisateur.Id)
                    .AsNoTracking()
                    .ToListAsync();

                var activite = invitations.Select(i => new ActiviteRecenteViewModel
                {
                    Titre = "Invitation envoyée",
                    Description = $"{i.Projet?.Titre} → {FormatNomUtilisateur(i.Entrepreneur?.Utilisateur, "Entrepreneur")} ({i.Statut})",
                    Date = i.DateCreation
                }).ToList();

                foreach (var projet in projets.Take(3))
                {
                    activite.Add(new ActiviteRecenteViewModel
                    {
                        Titre = "Projet",
                        Description = $"{projet.Titre} — {projet.Statut}",
                        Date = projet.DateCreation
                    });
                }

                var modele = new InvestisseurDashboardViewModel
                {
                    BonjourMessage = $"Bonjour, {FormatPrenom(utilisateur)}",
                    ProjetsActifs = activeProjects.Count,
                    JalonsEnAttente = pendingMilestones,
                    BudgetSousGestion = activeProjects.Sum(p => p.BudgetTotal),
                    TotalSequestre = sequestres.Sum(s => s.MontantBloque),
                    EntrepreneursActifs = entrepreneursActifs,
                    ActiviteRecente = activite.OrderByDescending(a => a.Date).Take(8).ToList(),
                    Projets = activeProjects.Select(p => new DashboardProjetViewModel
                    {
                        IdProjet = p.IdProjet,
                        Titre = p.Titre,
                        Investisseur = FormatNomUtilisateur(p.Entrepreneur?.Utilisateur, "À affecter"),
                        ProgressPourcentage = p.Jalons.Count == 0 ? 0 : (int)Math.Round((double)p.Jalons.Count(j => j.EstValide) / p.Jalons.Count * 100),
                        Statut = p.Statut.ToString(),
                        JalonsASoumettre = p.Jalons.Any(j => !j.EstValide)
                    }).ToList(),
                    Invitations = invitations.Select(i => new InvitationDashboardViewModel
                    {
                        IdInvitation = i.IdInvitation,
                        IdProjet = i.IdProjet,
                        ProjetTitre = i.Projet?.Titre ?? "Projet à définir",
                        EntrepreneurNom = FormatNomUtilisateur(i.Entrepreneur?.Utilisateur, "Entrepreneur à définir"),
                        Statut = i.Statut.ToString(),
                        Message = i.Message,
                        DateCreation = i.DateCreation
                    }).ToList()
                };

                return View("Investisseur", modele);
            }

            if (utilisateur.Role != RoleUtilisateur.Entrepreneur)
            {
                return RedirectToAction("MonProfil", "Profil");
            }

            var entrepreneur = await _contexte.Entrepreneurs
                .Include(e => e.Utilisateur)
                .FirstOrDefaultAsync(e => e.IdUtilisateur == utilisateur.Id);

            var projetsEntrepreneur = await _contexte.Projets
                .Where(p => entrepreneur != null && p.IdEntrepreneur == entrepreneur.IdEntrepreneur)
                .Include(p => p.Investisseur)
                .Include(p => p.Jalons)
                .OrderByDescending(p => p.DateCreation)
                .ToListAsync();

            var jalons = projetsEntrepreneur
                .SelectMany(p => p.Jalons)
                .Where(j => !j.EstValide)
                .OrderBy(j => j.DateLimite ?? DateTime.MaxValue)
                .Select(j => new DashboardJalonViewModel
                {
                    ProjetTitre = projetsEntrepreneur.First(p => p.Jalons.Any(jalon => jalon.IdJalon == j.IdJalon)).Titre,
                    Titre = j.Titre,
                    Montant = j.Montant,
                    DateLimite = j.DateLimite,
                    EstValide = j.EstValide
                })
                .ToList();

            var activeProjectsEntrepreneur = projetsEntrepreneur.Where(p => p.Statut is not StatutProjet.Termine and not StatutProjet.Annule).ToList();
            var pendingMilestonesEntrepreneur = projetsEntrepreneur.Sum(p => p.Jalons.Count(j => !j.EstValide));
            var aRecevoir = projetsEntrepreneur.Sum(p => p.Jalons.Where(j => !j.EstValide).Sum(j => j.Montant));

            var modeleEntrepreneur = new EntrepreneurDashboardViewModel
            {
                BonjourMessage = $"Bonjour, {FormatPrenom(utilisateur)}",
                PhotoProfilUrl = utilisateur.PhotoProfilUrl,
                Specialite = entrepreneur?.Specialite,
                Ville = entrepreneur?.Ville,
                CompteVerifie = utilisateur.Statut == StatutCompte.Actif,
                MoMo = entrepreneur?.NumeroMoMo,
                ScoreFiabilite = entrepreneur?.ScoreFilabilite ?? 0,
                ChantiersActifs = activeProjectsEntrepreneur.Count,
                JalonsASoumettre = pendingMilestonesEntrepreneur,
                ARecevoir = aRecevoir,
                Projets = activeProjectsEntrepreneur.Select(p => new DashboardProjetViewModel
                {
                    IdProjet = p.IdProjet,
                    Titre = p.Titre,
                    Investisseur = FormatNomInvestisseur(p.Investisseur),
                    ProgressPourcentage = p.Jalons.Count == 0 ? 0 : (int)Math.Round((double)p.Jalons.Count(j => j.EstValide) / p.Jalons.Count * 100),
                    Statut = p.Statut.ToString(),
                    JalonsASoumettre = p.Jalons.Any(j => !j.EstValide)
                }).ToList(),
                Jalons = jalons,
                Photos = new(),
                Materiaux = new(),
                Messages = new()
            };

            return View("Entrepreneur", modeleEntrepreneur);
        }

        [HttpGet]
        public async Task<IActionResult> Alertes()
        {
            var utilisateur = await _userManager.GetUserAsync(User);
            if (utilisateur is null)
            {
                return RedirectToAction("Connexion", "Auth");
            }

            if (utilisateur.Role == RoleUtilisateur.Administrateur)
            {
                return RedirectToAction("Index", "Admin");
            }

            var modele = new AlertesViewModel();

            if (utilisateur.Role == RoleUtilisateur.Investisseur)
            {
                var projetsIds = await _contexte.Projets
                    .Where(p => p.IdInvestisseur == utilisateur.Id)
                    .Select(p => p.IdProjet)
                    .ToListAsync();

                modele.JalonsAValider = await _contexte.Jalons
                    .CountAsync(j => projetsIds.Contains(j.IdProjet) && !j.EstValide);

                modele.PhotosSuspectes = await _contexte.Photos
                    .CountAsync(p => projetsIds.Contains(p.IdProjet) && !p.EstAuthentique);

                modele.InvitationsEnAttente = await _contexte.Invitations
                    .CountAsync(i => i.Projet != null && i.Projet.IdInvestisseur == utilisateur.Id && i.Statut == StatutInvitation.EnAttente);
            }
            else if (utilisateur.Role == RoleUtilisateur.Entrepreneur)
            {
                var entrepreneur = await _contexte.Entrepreneurs.FirstOrDefaultAsync(e => e.IdUtilisateur == utilisateur.Id);
                if (entrepreneur is not null)
                {
                    modele.InvitationsEnAttente = await _contexte.Invitations
                        .CountAsync(i => i.IdEntrepreneur == entrepreneur.IdEntrepreneur && i.Statut == StatutInvitation.EnAttente);

                    var projetsIds = await _contexte.Projets
                        .Where(p => p.IdEntrepreneur == entrepreneur.IdEntrepreneur)
                        .Select(p => p.IdProjet)
                        .ToListAsync();

                    modele.JalonsAValider = await _contexte.Jalons
                        .CountAsync(j => projetsIds.Contains(j.IdProjet) && !j.EstValide);
                }
            }

            return View(modele);
        }

        private static string FormatPrenom(Utilisateur utilisateur)
        {
            if (!string.IsNullOrWhiteSpace(utilisateur.Prenom))
            {
                return utilisateur.Prenom;
            }

            return utilisateur.UserName ?? "Utilisateur";
        }

        private static string FormatNomInvestisseur(Utilisateur? investisseur)
        {
            return FormatNomUtilisateur(investisseur, "Investisseur à définir");
        }

        private static string FormatNomUtilisateur(Utilisateur? utilisateur, string valeurParDefaut)
        {
            if (utilisateur is null)
            {
                return valeurParDefaut;
            }

            var blocs = new[] { utilisateur.Prenom, utilisateur.Nom }
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim());

            return blocs.Any() ? string.Join(" ", blocs) : utilisateur.Email ?? valeurParDefaut;
        }

    }
}