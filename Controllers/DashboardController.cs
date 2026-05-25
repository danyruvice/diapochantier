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

            if (utilisateur.Role != RoleUtilisateur.Entrepreneur)
            {
                return RedirectToAction("MonProfil", "Profil");
            }

            var entrepreneur = await _contexte.Entrepreneurs
                .Include(e => e.Utilisateur)
                .FirstOrDefaultAsync(e => e.IdUtilisateur == utilisateur.Id);

            var projets = await _contexte.Projets
                .Where(p => entrepreneur != null && p.IdEntrepreneur == entrepreneur.IdEntrepreneur)
                .Include(p => p.Investisseur)
                .Include(p => p.Jalons)
                .OrderByDescending(p => p.DateCreation)
                .ToListAsync();

            var jalons = projets
                .SelectMany(p => p.Jalons)
                .Where(j => !j.EstValide)
                .OrderBy(j => j.DateLimite ?? DateTime.MaxValue)
                .Select(j => new DashboardJalonViewModel
                {
                    ProjetTitre = projets.First(p => p.Jalons.Any(jalon => jalon.IdJalon == j.IdJalon)).Titre,
                    Titre = j.Titre,
                    Montant = j.Montant,
                    DateLimite = j.DateLimite,
                    EstValide = j.EstValide
                })
                .ToList();

            var activeProjects = projets.Where(p => p.Statut is not StatutProjet.Termine and not StatutProjet.Annule).ToList();
            var pendingMilestones = projets.Sum(p => p.Jalons.Count(j => !j.EstValide));
            var aRecevoir = projets.Sum(p => p.Jalons.Where(j => !j.EstValide).Sum(j => j.Montant));

            var prenom = string.IsNullOrWhiteSpace(utilisateur.Prenom)
                ? (utilisateur.UserName ?? "Paul")
                : utilisateur.Prenom;

            var modele = new EntrepreneurDashboardViewModel
            {
                BonjourMessage = $"Bonjour, {prenom}",
                PhotoProfilUrl = utilisateur.PhotoProfilUrl,
                Specialite = entrepreneur?.Specialite,
                Ville = entrepreneur?.Ville,
                CompteVerifie = utilisateur.Statut == StatutCompte.Actif,
                MoMo = entrepreneur?.NumeroMoMo,
                ScoreFiabilite = entrepreneur?.ScoreFilabilite ?? 0,
                ChantiersActifs = activeProjects.Count,
                JalonsASoumettre = pendingMilestones,
                ARecevoir = aRecevoir,
                Projets = activeProjects.Select(p => new DashboardProjetViewModel
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

            return View(modele);
        }

        private static string FormatNomInvestisseur(Utilisateur? investisseur)
        {
            if (investisseur is null)
            {
                return "Investisseur à définir";
            }

            var blocs = new[] { investisseur.Prenom, investisseur.Nom }
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim());

            return blocs.Any() ? string.Join(" ", blocs) : investisseur.Email ?? "Investisseur à définir";
        }

    }
}