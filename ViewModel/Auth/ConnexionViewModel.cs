using System.ComponentModel.DataAnnotations;

namespace DiaspoChantier.ViewModels
{
    public class ConnexionViewModel
    {
        [Required(ErrorMessage = "L'email est obligatoire.")]
        [EmailAddress(ErrorMessage = "Adresse email invalide.")]
        [Display(Name = "Adresse email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est obligatoire.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mot de passe")]
        public string MotDePasse { get; set; } = string.Empty;

        [Display(Name = "Se souvenir de moi")]
        public bool SesouveniDeMoi { get; set; }
    }
}
