using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DiaspoChantier.Enums;

namespace DiaspoChantier.Models
{
    public class Projet
    {
        [Key]
        public int IdProjet { get; set; }

        [Required]
        [StringLength(150)]
        public string Titre { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string TypeProjet { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Localisation { get; set; } = string.Empty;

        [Range(-90, 90)]
        public double Latitude { get; set; }

        [Range(-180, 180)]
        public double Longitude { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue)]
        public decimal BudgetTotal { get; set; }

        public StatutProjet Statut { get; set; } = StatutProjet.Brouillon;

        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        public DateTime DateMiseAJour { get; set; } = DateTime.UtcNow;

        [Required]
        public string IdInvestisseur { get; set; } = string.Empty;

        [ForeignKey(nameof(IdInvestisseur))]
        public Utilisateur? Investisseur { get; set; }

        public int? IdEntrepreneur { get; set; }

        [ForeignKey(nameof(IdEntrepreneur))]
        public Entrepreneur? Entrepreneur { get; set; }

        public ICollection<Jalon> Jalons { get; set; } = new List<Jalon>();
    }
}