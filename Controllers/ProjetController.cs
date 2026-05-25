using System.Globalization;
using DiaspoChantier.Data;
using DiaspoChantier.Enums;
using DiaspoChantier.Models;
using DiaspoChantier.Services;
using DiaspoChantier.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DiaspoChantier.Controllers;

[Authorize]
public class ProjetController : Controller
{
    private readonly ApplicationDbContext _contexte;
    private readonly UserManager<Utilisateur> _gestionnaireUtilisateurs;
    private readonly ServiceStockage _serviceStockage;
    private readonly PaiementService _paiementService;
    private readonly ProjetService _projetService;

    public ProjetController(ApplicationDbContext contexte, UserManager<Utilisateur> gestionnaireUtilisateurs, ServiceStockage serviceStockage, PaiementService paiementService, ProjetService projetService)
    {
        _contexte = contexte;
        _gestionnaireUtilisateurs = gestionnaireUtilisateurs;
        _serviceStockage = serviceStockage;
        _paiementService = paiementService;
        _projetService = projetService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var utilisateur = await ObtenirUtilisateurActuelAsync();
        if (utilisateur is null)
        {
            return RedirectToAction("Connexion", "Auth");
        }

        var estEntrepreneur = utilisateur.Role == RoleUtilisateur.Entrepreneur;
        var idEntrepreneur = await ObtenirIdEntrepreneurAsync(utilisateur);

        IQueryable<Projet> requete = _contexte.Projets.Include(p => p.Jalons);

        if (estEntrepreneur)
        {
            if (!idEntrepreneur.HasValue)
            {
                return View(new TableauBordProjetViewModel { EstEntrepreneur = true });
            }

            requete = requete.Where(p => p.IdEntrepreneur == idEntrepreneur.Value);
        }
        else if (utilisateur.Role == RoleUtilisateur.Investisseur)
        {
            requete = requete.Where(p => p.IdInvestisseur == utilisateur.Id);
        }
        else
        {
            return Forbid();
        }

        var projets = await requete
            .OrderByDescending(p => p.DateCreation)
            .AsNoTracking()
            .ToListAsync();

        var statistiques = new TableauBordProjetViewModel
        {
            EstEntrepreneur = estEntrepreneur,
            TotalProjets = projets.Count,
            ProjetsActifs = projets.Count(p => p.Statut is not StatutProjet.Termine and not StatutProjet.Annule),
            JalonsEnAttente = projets.Sum(p => p.Jalons.Count(j => !j.EstValide)),
            Projets = projets.Select(p => new ElementProjetViewModel
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
            }).ToList()
        };

        return View(statistiques);
    }

    [HttpGet]
    public async Task<IActionResult> Creer()
    {
        var utilisateur = await ObtenirUtilisateurActuelAsync();
        if (utilisateur is null)
        {
            return RedirectToAction("Connexion", "Auth");
        }

        if (utilisateur.Role is not RoleUtilisateur.Investisseur and not RoleUtilisateur.Administrateur)
        {
            return Forbid();
        }

        return View(new ModeleCreationProjet());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Creer(ModeleCreationProjet modele)
    {
        var utilisateur = await ObtenirUtilisateurActuelAsync();
        if (utilisateur is null)
        {
            return RedirectToAction("Connexion", "Auth");
        }

        if (utilisateur.Role is not RoleUtilisateur.Investisseur and not RoleUtilisateur.Administrateur)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(modele);
        }

        if (modele.Jalons.Count > 10)
        {
            ModelState.AddModelError(string.Empty, "Vous pouvez ajouter jusqu’à 10 jalons maximum.");
            return View(modele);
        }

        if (!EstDansLaZoneCameroun(modele.Latitude, modele.Longitude))
        {
            ModelState.AddModelError(string.Empty, "La localisation doit être située au Cameroun. Veuillez vérifier la latitude et la longitude.");
            return View(modele);
        }

        var jalonsValides = new List<ModeleJalonProjet>();
        for (var index = 0; index < modele.Jalons.Count; index++)
        {
            var jalon = modele.Jalons[index];
            if (!ContientInformationsJalon(jalon))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(jalon.Titre))
            {
                ModelState.AddModelError($"Jalons[{index}].Titre", "Le titre du jalon est obligatoire.");
            }

            if (jalon.Montant < 0)
            {
                ModelState.AddModelError($"Jalons[{index}].Montant", "Le montant ne peut pas être négatif.");
            }

            jalonsValides.Add(jalon);
        }

        if (jalonsValides.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Ajoutez au moins un jalon avec un titre ou un montant pour créer le projet.");
        }

        var totalJalons = jalonsValides.Sum(j => j.Montant);
        if (totalJalons > modele.BudgetTotal)
        {
            ModelState.AddModelError(string.Empty, $"La somme des jalons ({totalJalons:N0}) ne peut pas dépasser le budget total ({modele.BudgetTotal:N0}).");
        }

        if (!ModelState.IsValid)
        {
            return View(modele);
        }

        var projet = new Projet
        {
            Titre = modele.Titre.Trim(),
            TypeProjet = modele.TypeProjet.Trim(),
            Localisation = modele.Localisation.Trim(),
            Latitude = modele.Latitude,
            Longitude = modele.Longitude,
            BudgetTotal = modele.BudgetTotal,
            Statut = StatutProjet.Brouillon,
            DateCreation = DateTime.UtcNow,
            DateMiseAJour = DateTime.UtcNow,
            IdInvestisseur = utilisateur.Id,
            Jalons = jalonsValides.Select((jalon, index) => new Jalon
            {
                Titre = jalon.Titre.Trim(),
                Description = string.IsNullOrWhiteSpace(jalon.Description) ? null : jalon.Description.Trim(),
                Montant = jalon.Montant,
                Ordre = index + 1,
                DateLimite = jalon.DateLimite?.ToUniversalTime(),
                EstValide = false
            }).ToList()
        };

        _contexte.Projets.Add(projet);
        await _contexte.SaveChangesAsync();

        TempData["MessageSucces"] = "Le projet a été créé avec succès. Vous pouvez maintenant suivre ses jalons.";
        return RedirectToAction(nameof(Details), new { id = projet.IdProjet });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var utilisateur = await ObtenirUtilisateurActuelAsync();
        if (utilisateur is null)
        {
            return RedirectToAction("Connexion", "Auth");
        }

        var idEntrepreneur = await ObtenirIdEntrepreneurAsync(utilisateur);
        if (utilisateur.Role == RoleUtilisateur.Entrepreneur && !idEntrepreneur.HasValue)
        {
            return Forbid();
        }

        var projet = await _contexte.Projets
            .Where(p => p.IdProjet == id && (p.IdInvestisseur == utilisateur.Id || (idEntrepreneur.HasValue && p.IdEntrepreneur == idEntrepreneur.Value)))
            .Include(p => p.Jalons)
            .Include(p => p.Entrepreneur)
                .ThenInclude(e => e!.Utilisateur)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (projet is null)
        {
            return NotFound();
        }

        var photos = await _contexte.Photos
            .Where(p => p.IdProjet == id)
            .Include(p => p.Jalon)
            .AsNoTracking()
            .ToListAsync();

        var vue = await ConstruireDetailProjetViewModelAsync(projet, photos, utilisateur.Role == RoleUtilisateur.Investisseur);
        return View(vue);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = nameof(RoleUtilisateur.Investisseur))]
    public async Task<IActionResult> ValiderJalon(int id)
    {
        var utilisateur = await ObtenirUtilisateurActuelAsync();
        if (utilisateur is null)
        {
            return RedirectToAction("Connexion", "Auth");
        }

        var jalon = await _contexte.Jalons
            .Include(j => j.Projet)
            .FirstOrDefaultAsync(j => j.IdJalon == id && j.Projet!.IdInvestisseur == utilisateur.Id);

        if (jalon is null)
        {
            return NotFound();
        }

        var resultat = await _paiementService.LibererPaiementPourJalonAsync(id);
        if (!resultat.IsSuccess)
        {
            TempData["MessageErreur"] = resultat.Message;
            return RedirectToAction(nameof(Details), new { id = jalon.IdProjet });
        }

        TempData["MessageSucces"] = $"Le jalon \"{jalon.Titre}\" a été validé et le paiement a été libéré.";
        return RedirectToAction(nameof(Details), new { id = jalon.IdProjet });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = nameof(RoleUtilisateur.Investisseur))]
    public async Task<IActionResult> Annuler(int id)
    {
        var utilisateur = await ObtenirUtilisateurActuelAsync();
        if (utilisateur is null)
        {
            return RedirectToAction("Connexion", "Auth");
        }

        var projet = await _contexte.Projets.FirstOrDefaultAsync(p => p.IdProjet == id && p.IdInvestisseur == utilisateur.Id);
        if (projet is null)
        {
            return NotFound();
        }

        var resultat = await _projetService.AnnulerProjetAsync(id);
        TempData[resultat.IsSuccess ? "MessageSucces" : "MessageErreur"] = resultat.IsSuccess
            ? "Le projet a été annulé."
            : resultat.Message;

        return RedirectToAction(nameof(Index));
    }

    private async Task<DetailProjetViewModel> ConstruireDetailProjetViewModelAsync(Projet projet, IEnumerable<Photo>? photos = null, bool peutValiderJalons = false)
    {
        var photosPersistees = (photos ?? Enumerable.Empty<Photo>()).ToList();
        var photosViewModel = photosPersistees.Select(photo => new PhotoProjetViewModel
        {
            IdPhoto = photo.IdPhoto,
            IdJalon = photo.IdJalon,
            JalonTitre = photo.Jalon?.Titre ?? "Jalon non identifié",
            Url = photo.Url,
            NomFichier = Path.GetFileName(photo.Url),
            Horodatage = photo.HorodatageGps.ToString("g"),
            Latitude = photo.Latitude.ToString("0.####", CultureInfo.InvariantCulture),
            Longitude = photo.Longitude.ToString("0.####", CultureInfo.InvariantCulture),
            Altitude = photo.Altitude?.ToString("0.####", CultureInfo.InvariantCulture),
            Distance = FormaterDistance(CalculerDistanceEnMetres(photo.Latitude, photo.Longitude, projet.Latitude, projet.Longitude)),
            EstAuthentique = photo.EstAuthentique,
            Source = photo.Source,
            HashSHA256 = photo.HashSHA256
        }).ToList();

        return new DetailProjetViewModel
        {
            PeutValiderJalons = peutValiderJalons,
            IdProjet = projet.IdProjet,
            Titre = projet.Titre,
            TypeProjet = projet.TypeProjet,
            Localisation = projet.Localisation,
            Latitude = projet.Latitude,
            Longitude = projet.Longitude,
            BudgetTotal = projet.BudgetTotal,
            Statut = projet.Statut.ToString(),
            DateCreation = projet.DateCreation,
            Entrepreneur = projet.Entrepreneur?.Utilisateur is null
                ? null
                : $"{projet.Entrepreneur.Utilisateur.Prenom} {projet.Entrepreneur.Utilisateur.Nom}".Trim(),
            Jalons = projet.Jalons
                .OrderBy(j => j.Ordre)
                .Select(j => new DetailJalonViewModel
                {
                    IdJalon = j.IdJalon,
                    Titre = j.Titre,
                    Description = j.Description,
                    Montant = j.Montant,
                    Ordre = j.Ordre,
                    DateLimite = j.DateLimite,
                    EstValide = j.EstValide,
                    DateValidation = j.DateValidation,
                    PeutValider = peutValiderJalons && photosViewModel.Any(p => p.IdJalon == j.IdJalon && p.EstAuthentique),
                    Photos = photosViewModel.Where(p => p.IdJalon == j.IdJalon).ToList()
                }).ToList(),
            Photos = photosViewModel
        };
    }

    private async Task<int?> ObtenirIdEntrepreneurAsync(Utilisateur utilisateur)
    {
        if (utilisateur.Role != RoleUtilisateur.Entrepreneur)
        {
            return null;
        }

        var entrepreneur = await _contexte.Entrepreneurs
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.IdUtilisateur == utilisateur.Id);

        return entrepreneur?.IdEntrepreneur;
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

    private static bool ContientInformationsJalon(ModeleJalonProjet jalon)
    {
        return !string.IsNullOrWhiteSpace(jalon.Titre)
            || !string.IsNullOrWhiteSpace(jalon.Description)
            || jalon.Montant > 0
            || jalon.DateLimite.HasValue;
    }

    private static bool EstDansLaZoneCameroun(double latitude, double longitude)
    {
        const double minLatitude = 1.9;
        const double maxLatitude = 13.5;
        const double minLongitude = 8.4;
        const double maxLongitude = 16.2;

        return latitude >= minLatitude
            && latitude <= maxLatitude
            && longitude >= minLongitude
            && longitude <= maxLongitude;
    }

    private static double CalculerDistanceEnMetres(double latitudePhoto, double longitudePhoto, double latitudeProjet, double longitudeProjet)
    {
        const double rayonTerre = 6371000d;
        var latitude1 = DegreesToRadians(latitudePhoto);
        var latitude2 = DegreesToRadians(latitudeProjet);
        var differenceLatitude = DegreesToRadians(latitudeProjet - latitudePhoto);
        var differenceLongitude = DegreesToRadians(longitudeProjet - longitudePhoto);

        var a = Math.Sin(differenceLatitude / 2) * Math.Sin(differenceLatitude / 2)
            + Math.Cos(latitude1) * Math.Cos(latitude2)
            * Math.Sin(differenceLongitude / 2) * Math.Sin(differenceLongitude / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return rayonTerre * c;
    }

    private static string FormaterDistance(double distanceMetres)
    {
        return distanceMetres < 1000
            ? $"{Math.Round(distanceMetres)} m"
            : $"{distanceMetres / 1000d:F1} km";
    }

    private static double DegreesToRadians(double degre)
    {
        return degre * Math.PI / 180d;
    }
}
