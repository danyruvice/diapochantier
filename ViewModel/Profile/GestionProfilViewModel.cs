using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace DiaspoChantier.ViewModels
{
    public class GestionProfilViewModel
    {
        [Required(ErrorMessage = "Le prénom est obligatoire.")]
        [StringLength(100)]
        [Display(Name = "Prénom")]
        public string Prenom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nom est obligatoire.")]
        [StringLength(100)]
        [Display(Name = "Nom")]
        public string Nom { get; set; } = string.Empty;

        [Display(Name = "Adresse email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le téléphone est obligatoire.")]
        [Phone]
        [Display(Name = "Téléphone")]
        public string Telephone { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Adresse")]
        public string? Adresse { get; set; }

        [Display(Name = "Photo de profil")]
        public IFormFile? PhotoProfil { get; set; }

        public string? PhotoProfilUrlActuelle { get; set; }

        [Display(Name = "Spécialité")]
        public string? Specialite { get; set; }

        [Display(Name = "Ville")]
        public string? Ville { get; set; }

        [Display(Name = "Numéro MTN MoMo")]
        public string? NumeroMoMo { get; set; }

        public string RoleAffiche { get; set; } = string.Empty;
        public string StatutAffiche { get; set; } = string.Empty;
    }
}
