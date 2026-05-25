using System.ComponentModel.DataAnnotations;

namespace DiaspoChantier.ViewModels
{
    public class InscriptionInvestisseurViewModel
    {
        [Required(ErrorMessage = "Le prénom est obligatoire.")]
        [StringLength(100, ErrorMessage = "Le prénom ne peut pas dépasser 100 caractères.")]
        [Display(Name = "Prénom")]
        public string Prenom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nom est obligatoire.")]
        [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères.")]
        [Display(Name = "Nom")]
        public string Nom { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'email est obligatoire.")]
        [EmailAddress(ErrorMessage = "Adresse email invalide.")]
        [Display(Name = "Adresse email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le numéro de téléphone est obligatoire.")]
        [Phone(ErrorMessage = "Numéro de téléphone invalide.")]
        [Display(Name = "Téléphone (ex: +1 514 000-0000)")]
        public string Telephone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le pays de résidence est obligatoire.")]
        [Display(Name = "Pays de résidence")]
        public string PaysResidence { get; set; } = string.Empty;

        [Required(ErrorMessage = "La pièce d'identité est obligatoire.")]
        [Display(Name = "Pièce d'identité (PDF ou image)")]
        public IFormFile? FichierPieceIdentite { get; set; }

        [Required(ErrorMessage = "Le mot de passe est obligatoire.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Le mot de passe doit comporter au moins 8 caractères.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mot de passe")]
        public string MotDePasse { get; set; } = string.Empty;

        [Required(ErrorMessage = "La confirmation du mot de passe est obligatoire.")]
        [Compare(nameof(MotDePasse), ErrorMessage = "Les mots de passe ne correspondent pas.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmer le mot de passe")]
        public string ConfirmerMotDePasse { get; set; } = string.Empty;

        [Display(Name = "J'accepte les conditions d'utilisation")]
        [MustBeTrue(ErrorMessage = "Vous devez accepter les conditions d'utilisation.")]
        public bool AccepterConditions { get; set; }
    }
}
