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
public class PhotoController : Controller
{
    private readonly ApplicationDbContext _contexte;
    private readonly UserManager<Utilisateur> _gestionnaireUtilisateurs;
    private readonly ServiceStockage _serviceStockage;
    private readonly PhotoService _photoService;

    public PhotoController(ApplicationDbContext contexte, UserManager<Utilisateur> gestionnaireUtilisateurs, ServiceStockage serviceStockage, PhotoService photoService)
    {
        _contexte = contexte;
        _gestionnaireUtilisateurs = gestionnaireUtilisateurs;
        _serviceStockage = serviceStockage;
        _photoService = photoService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(int projetId, int jalonId, IFormFile? image, string? latitude, string? longitude, string? altitude, string? hashSha256, string? horodatageGps)
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
            .Where(p => p.IdProjet == projetId && (p.IdInvestisseur == utilisateur.Id || (idEntrepreneur.HasValue && p.IdEntrepreneur == idEntrepreneur.Value)))
            .Include(p => p.Jalons)
            .FirstOrDefaultAsync();

        if (projet is null)
        {
            return NotFound();
        }

        var jalon = await _contexte.Jalons.FirstOrDefaultAsync(j => j.IdJalon == jalonId && j.IdProjet == projet.IdProjet);
        if (jalon is null)
        {
            TempData["MessageErreur"] = "Le jalon sélectionné est introuvable.";
            return RedirectToAction("Details", "Projet", new { id = projetId });
        }

        if (image is null || image.Length == 0)
        {
            TempData["MessageErreur"] = "Veuillez sélectionner une photo à téléverser.";
            return RedirectToAction("Details", "Projet", new { id = projetId });
        }

        if (!double.TryParse(latitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var latitudePhoto)
            || !double.TryParse(longitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var longitudePhoto))
        {
            TempData["MessageErreur"] = "La position GPS est obligatoire pour vérifier l’authenticité de la photo.";
            return RedirectToAction("Details", "Projet", new { id = projetId });
        }

        if (!PhotoService.EstDansLaZoneCameroun(latitudePhoto, longitudePhoto))
        {
            TempData["MessageErreur"] = "La position GPS doit être située au Cameroun.";
            return RedirectToAction("Details", "Projet", new { id = projetId });
        }

        if (!double.TryParse(altitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var altitudePhoto))
        {
            altitudePhoto = 0;
        }

        if (!DateTimeOffset.TryParse(horodatageGps, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var horodatage))
        {
            horodatage = DateTimeOffset.UtcNow;
        }

        try
        {
            var resultat = await _serviceStockage.EnregistrerAsync(image, $"projets/{projetId}/photos", hashSha256);

            var distance = _photoService.CalculerDistanceEnMetres(latitudePhoto, longitudePhoto, projet.Latitude, projet.Longitude);
            var estAuthentique = distance <= 200;

            var photo = new Photo
            {
                IdProjet = projet.IdProjet,
                IdJalon = jalon.IdJalon,
                Url = resultat.Url,
                HashSHA256 = resultat.HashSHA256,
                Latitude = latitudePhoto,
                Longitude = longitudePhoto,
                Altitude = altitudePhoto,
                HorodatageGps = horodatage.UtcDateTime,
                EstAuthentique = estAuthentique,
                Source = "Caméra mobile",
                DateCreation = DateTime.UtcNow
            };

            _contexte.Photos.Add(photo);
            await _contexte.SaveChangesAsync();

            if (!estAuthentique)
            {
                await _photoService.AlerterSuspicionAsync(projet, jalon, distance, latitudePhoto, longitudePhoto);
                TempData["MessageErreur"] = "La photo a bien été téléversée, mais elle est marquée suspecte car elle est située à plus de 200 m du chantier.";
            }
            else
            {
                TempData["MessageSucces"] = "La photo a été capturée et vérifiée comme authentique.";
            }
        }
        catch (InvalidOperationException ex)
        {
            TempData["MessageErreur"] = ex.Message;
        }

        return RedirectToAction("Details", "Projet", new { id = projetId });
    }

    [HttpGet]
    public async Task<IActionResult> Galerie(int projetId)
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
            .Where(p => p.IdProjet == projetId && (p.IdInvestisseur == utilisateur.Id || (idEntrepreneur.HasValue && p.IdEntrepreneur == idEntrepreneur.Value)))
            .Include(p => p.Jalons)
            .FirstOrDefaultAsync();

        if (projet is null)
        {
            return NotFound();
        }

        var photos = await _contexte.Photos
            .Where(p => p.IdProjet == projetId)
            .Include(p => p.Jalon)
            .OrderByDescending(p => p.DateCreation)
            .AsNoTracking()
            .ToListAsync();

        var vue = await ConstruireDetailProjetViewModelAsync(projet, photos);
        return View(vue);
    }

    private async Task<DetailProjetViewModel> ConstruireDetailProjetViewModelAsync(Projet projet, IEnumerable<Photo> photos)
    {
        var photoListe = photos.ToList();
        var photoViewModels = photoListe.Select(photo =>
        {
            var distance = _photoService.CalculerDistanceEnMetres(photo.Latitude, photo.Longitude, projet.Latitude, projet.Longitude);
            return new PhotoProjetViewModel
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
                Distance = distance <= 200 ? "À moins de 200 m" : PhotoService.FormaterDistance(distance),
                EstAuthentique = photo.EstAuthentique,
                Source = photo.Source,
                HashSHA256 = photo.HashSHA256
            };
        }).ToList();

        return new DetailProjetViewModel
        {
            PeutValiderJalons = false,
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
                    PeutValider = photoViewModels.Any(p => p.IdJalon == j.IdJalon && p.EstAuthentique),
                    Photos = photoViewModels.Where(p => p.IdJalon == j.IdJalon).ToList()
                }).ToList(),
            Photos = photoViewModels
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
}
