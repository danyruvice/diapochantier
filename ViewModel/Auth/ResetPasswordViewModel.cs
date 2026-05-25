using System.ComponentModel.DataAnnotations;

namespace DiaspoChantier.ViewModels
{
    public class ResetPasswordViewModel
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nouveau mot de passe est obligatoire.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Au moins 8 caractères avec majuscule et chiffre.")]
        [DataType(DataType.Password)]
        [Display(Name = "Nouveau mot de passe")]
        public string NouveauMotDePasse { get; set; } = string.Empty;

        [Required(ErrorMessage = "La confirmation est obligatoire.")]
        [Compare(nameof(NouveauMotDePasse), ErrorMessage = "Les mots de passe ne correspondent pas.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmer le nouveau mot de passe")]
        public string ConfirmerNouveauMotDePasse { get; set; } = string.Empty;
    }
}
