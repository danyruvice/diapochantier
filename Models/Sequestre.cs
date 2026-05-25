using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DiaspoChantier.Enums;

namespace DiaspoChantier.Models;

public class Sequestre
{
    [Key]
    public int IdSequestre { get; set; }

    [Required]
    public int IdProjet { get; set; }

    [Required]
    public string IdInvestisseur { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal MontantBloque { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal MontantLibere { get; set; }

    public StatutSequestre Statut { get; set; } = StatutSequestre.Bloque;

    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    public DateTime DateMiseAJour { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(IdProjet))]
    public Projet? Projet { get; set; }

    [ForeignKey(nameof(IdInvestisseur))]
    public Utilisateur? Investisseur { get; set; }
}
