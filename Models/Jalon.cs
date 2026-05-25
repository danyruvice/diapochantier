using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiaspoChantier.Models
{
    public class Jalon
    {
        [Key]
        public int IdJalon { get; set; }

        [Required]
        [StringLength(150)]
        public string Titre { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        public decimal Montant { get; set; }

        public int Ordre { get; set; } = 1;

        public DateTime? DateLimite { get; set; }

        public bool EstValide { get; set; }

        public DateTime? DateValidation { get; set; }

        public int IdProjet { get; set; }

        [ForeignKey(nameof(IdProjet))]
        public Projet? Projet { get; set; }
    }
}