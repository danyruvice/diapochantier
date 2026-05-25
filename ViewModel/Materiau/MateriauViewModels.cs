using System.ComponentModel.DataAnnotations;

namespace DiaspoChantier.ViewModels;

public class AjoutMateriauViewModel
{
    public int IdProjet { get; set; }

    [Required]
    [Display(Name = "Fournisseur")]
    public string Fournisseur { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Désignation")]
    public string Designation { get; set; } = string.Empty;

    [Required]
    [Range(0.01, double.MaxValue)]
    [Display(Name = "Quantité")]
    public decimal Quantite { get; set; }

    [Required]
    [Display(Name = "Unité")]
    public string Unite { get; set; } = "unité";

    [Required]
    [Range(0.01, double.MaxValue)]
    [Display(Name = "Prix total (FCFA)")]
    public decimal PrixTotal { get; set; }

    [Required]
    [Display(Name = "Photo facture")]
    public IFormFile? Facture { get; set; }

    public string? HashFacture { get; set; }
}
