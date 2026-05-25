using System.Globalization;
using DiaspoChantier.Data;
using DiaspoChantier.Enums;
using DiaspoChantier.Models;
using DiaspoChantier.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DiaspoChantier.Controllers.Api;

[ApiController]
[Route("api")]
[Authorize]
public class PhotoApiController : ControllerBase
{
    private readonly ApplicationDbContext _contexte;
    private readonly UserManager<Utilisateur> _users;
    private readonly ServiceStockage _stockage;
    private readonly PhotoService _photoService;

    public PhotoApiController(ApplicationDbContext contexte, UserManager<Utilisateur> users, ServiceStockage stockage, PhotoService photoService)
    {
        _contexte = contexte;
        _users = users;
        _stockage = stockage;
        _photoService = photoService;
    }

    [HttpPost("photos/upload")]
    [Authorize(Roles = nameof(RoleUtilisateur.Entrepreneur))]
    [RequestSizeLimit(15_000_000)]
    public async Task<IActionResult> Upload(
        [FromForm] int projetId,
        [FromForm] int jalonId,
        [FromForm] IFormFile? image,
        [FromForm] string? latitude,
        [FromForm] string? longitude,
        [FromForm] string? altitude,
        [FromForm] string? hashSha256,
        [FromForm] string? horodatageGps)
    {
        var utilisateur = await _users.GetUserAsync(User);
        if (utilisateur is null)
        {
            return Unauthorized();
        }

        var entrepreneur = await _contexte.Entrepreneurs.FirstOrDefaultAsync(e => e.IdUtilisateur == utilisateur.Id);
        if (entrepreneur is null)
        {
            return Forbid();
        }

        var projet = await _contexte.Projets
            .Include(p => p.Jalons)
            .FirstOrDefaultAsync(p => p.IdProjet == projetId && p.IdEntrepreneur == entrepreneur.IdEntrepreneur);

        if (projet is null)
        {
            return NotFound(new { message = "Projet introuvable." });
        }

        if (image is null || image.Length == 0)
        {
            return BadRequest(new { message = "Image requise." });
        }

        if (!double.TryParse(latitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat)
            || !double.TryParse(longitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
        {
            return BadRequest(new { message = "GPS obligatoire." });
        }

        if (!PhotoService.EstDansLaZoneCameroun(lat, lon))
        {
            return BadRequest(new { message = "GPS hors Cameroun." });
        }

        try
        {
            var resultat = await _stockage.EnregistrerAsync(image, $"projets/{projetId}/photos", hashSha256);
            var distance = _photoService.CalculerDistanceEnMetres(lat, lon, projet.Latitude, projet.Longitude);
            var authentique = distance <= 200;

            if (!DateTimeOffset.TryParse(horodatageGps, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var horodatage))
            {
                horodatage = DateTimeOffset.UtcNow;
            }

            var photo = new Photo
            {
                IdProjet = projetId,
                IdJalon = jalonId,
                Url = resultat.Url,
                HashSHA256 = resultat.HashSHA256,
                Latitude = lat,
                Longitude = lon,
                Altitude = double.TryParse(altitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var alt) ? alt : null,
                HorodatageGps = horodatage.UtcDateTime,
                EstAuthentique = authentique,
                Source = "Caméra mobile",
                DateCreation = DateTime.UtcNow
            };

            _contexte.Photos.Add(photo);
            await _contexte.SaveChangesAsync();

            var jalon = projet.Jalons.FirstOrDefault(j => j.IdJalon == jalonId);
            if (!authentique && jalon is not null)
            {
                await _photoService.AlerterSuspicionAsync(projet, jalon, distance, lat, lon);
                photo.AlerteEnvoyee = true;
                await _contexte.SaveChangesAsync();
            }

            return Ok(new
            {
                photo.IdPhoto,
                photo.EstAuthentique,
                statut = authentique ? "Authentique" : "Suspecte",
                distanceMetres = Math.Round(distance)
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("jalons/{id:int}/photos")]
    public async Task<IActionResult> PhotosJalon(int id)
    {
        var photos = await _contexte.Photos
            .Where(p => p.IdJalon == id)
            .Include(p => p.Jalon)
            .Include(p => p.Projet)
            .AsNoTracking()
            .ToListAsync();

        if (photos.Count == 0)
        {
            return Ok(Array.Empty<object>());
        }

        var projet = photos[0].Projet;
        return Ok(photos.Select(photo =>
        {
            var distance = projet is null
                ? 0
                : _photoService.CalculerDistanceEnMetres(photo.Latitude, photo.Longitude, projet.Latitude, projet.Longitude);

            return new
            {
                photo.IdPhoto,
                photo.Url,
                photo.Latitude,
                photo.Longitude,
                photo.Altitude,
                horodatage = photo.HorodatageGps,
                photo.HashSHA256,
                estAuthentique = photo.EstAuthentique,
                statut = photo.EstAuthentique ? "Authentique" : "Suspecte",
                distance = PhotoService.FormaterDistance(distance)
            };
        }));
    }
}
