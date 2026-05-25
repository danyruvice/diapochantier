using DiaspoChantier.Data;
using DiaspoChantier.Enums;
using DiaspoChantier.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DiaspoChantier.Services;

public class PhotoService
{
    private readonly ApplicationDbContext _contexte;
    private readonly UserManager<Utilisateur> _gestionnaireUtilisateurs;
    private readonly ServiceEmail _serviceEmail;

    public PhotoService(ApplicationDbContext contexte, UserManager<Utilisateur> gestionnaireUtilisateurs, ServiceEmail serviceEmail)
    {
        _contexte = contexte;
        _gestionnaireUtilisateurs = gestionnaireUtilisateurs;
        _serviceEmail = serviceEmail;
    }

    public double CalculerDistanceEnMetres(double latitudePhoto, double longitudePhoto, double latitudeProjet, double longitudeProjet)
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

    public static bool EstDansLaZoneCameroun(double latitude, double longitude)
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

    public static string FormaterDistance(double distanceMetres)
    {
        return distanceMetres < 1000
            ? $"{Math.Round(distanceMetres)} m"
            : $"{distanceMetres / 1000d:F1} km";
    }

    public async Task AlerterSuspicionAsync(Projet projet, Jalon jalon, double distance, double latitude, double longitude)
    {
        var administrateurs = await _gestionnaireUtilisateurs.GetUsersInRoleAsync(nameof(RoleUtilisateur.Administrateur));
        if (administrateurs.Count == 0)
        {
            return;
        }

        var message = $"La photo du jalon \"{jalon.Titre}\" pour le projet \"{projet.Titre}\" a été marquée suspecte.\n" +
            $"Distance estimée : {FormaterDistance(distance)}\n" +
            $"Coordonnées capturées : {latitude:0.####}, {longitude:0.####}\n" +
            $"Merci de vérifier la prise de vue et contacter le porteur si nécessaire.";

        foreach (var administrateur in administrateurs)
        {
            if (string.IsNullOrWhiteSpace(administrateur.Email))
            {
                continue;
            }

            await _serviceEmail.EnvoyerAsync(
                administrateur.Email,
                "Alerte anti-fraude sur une photo de chantier",
                message);
        }
    }

    public static double DegreesToRadians(double degre)
    {
        return degre * Math.PI / 180d;
    }
}
