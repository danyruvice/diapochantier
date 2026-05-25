using DiaspoChantier.Enums;
using Microsoft.AspNetCore.Identity;

namespace DiaspoChantier.Models
{
    /// <summary>
    /// Classe mère — hérite de IdentityUser pour ASP.NET Core Identity.
    /// Tous les acteurs du système (Investisseur, Entrepreneur, Admin, Médiateur) héritent de cette classe.
    /// </summary>
    public class Utilisateur : IdentityUser
    {
        [PersonalData]
        public string Nom { get; set; } = string.Empty;

        [PersonalData]
        public string Prenom { get; set; } = string.Empty;

        [PersonalData]
        public string Telephone { get; set; } = string.Empty;

        [PersonalData]
        public string Adresse { get; set; } = string.Empty;

        public RoleUtilisateur Role { get; set; } = RoleUtilisateur.Investisseur;

        public StatutCompte Statut { get; set; } = StatutCompte.AttenteVerification;

        public string? PhotoProfilUrl { get; set; }

        public DateTime DateInscription { get; set; } = DateTime.UtcNow;
        public DateTime? DerniereConnexion { get; set; }

        public Entrepreneur? ProfilEntrepreneur { get; set; }
    }
}