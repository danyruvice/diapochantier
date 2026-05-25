using System.ComponentModel.DataAnnotations;

namespace DiaspoChantier.ViewModels
{
    public class InscriptionEntrepreneurViewModel
    {
        [Required(ErrorMessage = "Le prénom est obligatoire.")]
        [StringLength(100)]
        [Display(Name = "Prénom")]
        public string Prenom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nom est obligatoire.")]
        [StringLength(100)]
        [Display(Name = "Nom")]
        public string Nom { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'email est obligatoire.")]
        [EmailAddress(ErrorMessage = "Adresse email invalide.")]
        [Display(Name = "Adresse email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le téléphone est obligatoire.")]
        [Phone]
        [Display(Name = "Téléphone (Cameroun)")]
        public string Telephone { get; set; } = string.Empty;

        [Required(ErrorMessage = "La ville est obligatoire.")]
        [Display(Name = "Ville (Cameroun)")]
        public string Ville { get; set; } = string.Empty;

        [Required(ErrorMessage = "La spécialité est obligatoire.")]
        [Display(Name = "Spécialité")]
        public string Specialite { get; set; } = string.Empty;

        [Display(Name = "Description (optionnel)")]
        [StringLength(500)]
        public string? Description { get; set; }

        [Display(Name = "Numéro MTN MoMo")]
        [StringLength(30)]
        public string? NumeroMoMo { get; set; }

        [Required(ErrorMessage = "La CNI est obligatoire.")]
        [Display(Name = "Carte Nationale d'Identité (CNI)")]
        public IFormFile? FichierCNI { get; set; }

        [Required(ErrorMessage = "La carte professionnelle est obligatoire.")]
        [Display(Name = "Carte professionnelle BTP")]
        public IFormFile? FichierCartePro { get; set; }

        [Display(Name = "Portfolio / Références (optionnel)")]
        public IFormFile? FichierPortfolio { get; set; }

        [Required(ErrorMessage = "Le mot de passe est obligatoire.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Au moins 8 caractères avec majuscule et chiffre.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mot de passe")]
        public string MotDePasse { get; set; } = string.Empty;

        [Required(ErrorMessage = "La confirmation est obligatoire.")]
        [Compare(nameof(MotDePasse), ErrorMessage = "Les mots de passe ne correspondent pas.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmer le mot de passe")]
        public string ConfirmerMotDePasse { get; set; } = string.Empty;

        [MustBeTrue(ErrorMessage = "Vous devez accepter les conditions.")]
        public bool AccepterConditions { get; set; }
    }
}
