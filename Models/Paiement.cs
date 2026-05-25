using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DiaspoChantier.Enums;

namespace DiaspoChantier.Models;

public class Paiement
{
    [Key]
    public int IdPaiement { get; set; }

    [Required]
    public int IdProjet { get; set; }

    [Required]
    public string IdInvestisseur { get; set; } = string.Empty;

    public int? IdEntrepreneur { get; set; }

    public int? IdSequestre { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Montant { get; set; }

    public TypePaiement TypePaiement { get; set; } = TypePaiement.Depot;

    public CanalPaiement Canal { get; set; } = CanalPaiement.Stripe;

    public StatutPaiement Statut { get; set; } = StatutPaiement.EnAttente;

    [StringLength(120)]
    public string ReferenceExterne { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Message { get; set; }

    public bool RecuEmailEnvoye { get; set; }

    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    public DateTime DateMiseAJour { get; set; } = DateTime.UtcNow;

    public DateTime? DatePaiement { get; set; }

    [ForeignKey(nameof(IdProjet))]
    public Projet? Projet { get; set; }

    [ForeignKey(nameof(IdInvestisseur))]
    public Utilisateur? Investisseur { get; set; }

    [ForeignKey(nameof(IdEntrepreneur))]
    public Entrepreneur? Entrepreneur { get; set; }

    [ForeignKey(nameof(IdSequestre))]
    public Sequestre? Sequestre { get; set; }
}
