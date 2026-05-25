using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiaspoChantier.Models;

public class Materiau
{
    [Key]
    public int IdMateriau { get; set; }

    public int IdProjet { get; set; }

    public int IdEntrepreneur { get; set; }

    [Required]
    [StringLength(150)]
    public string Fournisseur { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Designation { get; set; } = string.Empty;

    public decimal Quantite { get; set; }

    [Required]
    [StringLength(30)]
    public string Unite { get; set; } = string.Empty;

    public decimal PrixTotal { get; set; }

    [StringLength(500)]
    public string UrlFacture { get; set; } = string.Empty;

    [StringLength(64)]
    public string HashSHA256 { get; set; } = string.Empty;

    [StringLength(500)]
    public string QrCodePayload { get; set; } = string.Empty;

    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(IdProjet))]
    public Projet? Projet { get; set; }
}
