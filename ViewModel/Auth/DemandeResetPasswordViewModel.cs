using System.ComponentModel.DataAnnotations;

namespace DiaspoChantier.ViewModels
{
    public class DemandeResetPasswordViewModel
    {
        [Required(ErrorMessage = "L'email est obligatoire.")]
        [EmailAddress(ErrorMessage = "Adresse email invalide.")]
        [Display(Name = "Adresse email")]
        public string Email { get; set; } = string.Empty;
    }
}
