using DiaspoChantier.Data;
using Microsoft.EntityFrameworkCore;

namespace DiaspoChantier.Services;

/// <summary>
/// Vérifie périodiquement les photos non authentiques et déclenche les alertes admin.
/// </summary>
public class PhotosSuspectesBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<PhotosSuspectesBackgroundService> _logger;

    public PhotosSuspectesBackgroundService(IServiceProvider services, ILogger<PhotosSuspectesBackgroundService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TraiterPhotosSuspectesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du scan des photos suspectes.");
            }

            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }

    private async Task TraiterPhotosSuspectesAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var contexte = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var photoService = scope.ServiceProvider.GetRequiredService<PhotoService>();

        var photos = await contexte.Photos
            .Where(p => !p.EstAuthentique && !p.AlerteEnvoyee)
            .Include(p => p.Jalon)
            .Include(p => p.Projet)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var photo in photos)
        {
            if (photo.Projet is null || photo.Jalon is null)
            {
                continue;
            }

            var distance = photoService.CalculerDistanceEnMetres(
                photo.Latitude, photo.Longitude,
                photo.Projet.Latitude, photo.Projet.Longitude);

            await photoService.AlerterSuspicionAsync(photo.Projet, photo.Jalon, distance, photo.Latitude, photo.Longitude);
            photo.AlerteEnvoyee = true;
        }

        if (photos.Count > 0)
        {
            await contexte.SaveChangesAsync(cancellationToken);
        }
    }
}
