using DiaspoChantier.Enums;
using DiaspoChantier.Models;
using Microsoft.AspNetCore.Identity;

namespace DiaspoChantier.Middleware;

/// <summary>
/// Bloque l'accès aux fonctionnalités selon le statut du compte (surtout entrepreneurs non validés).
/// </summary>
public class VerificationCompteMiddleware
{
    private static readonly string[] CheminsPublics =
    [
        "/auth/",
        "/home/index",
        "/home/support",
        "/home/commentcamarche",
        "/home/confidentialite",
        "/home/privacy",
        "/home/error",
        "/materiau/verifier",
        "/api/",
        "/webhooks/",
        "/css/",
        "/js/",
        "/lib/",
        "/uploads/",
        "/favicon",
        "/logo_"
    ];

    private readonly RequestDelegate _suivant;

    public VerificationCompteMiddleware(RequestDelegate suivant)
    {
        _suivant = suivant;
    }

    public async Task InvokeAsync(HttpContext contexte, UserManager<Utilisateur> gestionnaireUtilisateurs)
    {
        if (contexte.User.Identity?.IsAuthenticated != true)
        {
            await _suivant(contexte);
            return;
        }

        var chemin = contexte.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        if (CheminsPublics.Any(c => chemin.StartsWith(c, StringComparison.Ordinal)))
        {
            await _suivant(contexte);
            return;
        }

        var utilisateur = await gestionnaireUtilisateurs.GetUserAsync(contexte.User);
        if (utilisateur is null)
        {
            await _suivant(contexte);
            return;
        }

        if (utilisateur.Role == RoleUtilisateur.Administrateur)
        {
            await _suivant(contexte);
            return;
        }

        if (utilisateur.Statut == StatutCompte.Suspendu)
        {
            contexte.Response.Redirect("/Auth/CompteDesactive");
            return;
        }

        if (utilisateur.Role == RoleUtilisateur.Entrepreneur)
        {
            if (utilisateur.Statut == StatutCompte.AttenteVerification
                && !chemin.StartsWith("/auth/compteenattente", StringComparison.Ordinal)
                && !chemin.StartsWith("/profil/monprofil", StringComparison.Ordinal))
            {
                contexte.Response.Redirect("/Auth/CompteEnAttente");
                return;
            }

            if (utilisateur.Statut == StatutCompte.Rejete
                && !chemin.StartsWith("/auth/compterejete", StringComparison.Ordinal)
                && !chemin.StartsWith("/profil/monprofil", StringComparison.Ordinal))
            {
                contexte.Response.Redirect("/Auth/CompteRejete");
                return;
            }
        }

        await _suivant(contexte);
    }
}
