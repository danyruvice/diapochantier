using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiaspoChantier.Models
{
    /// <summary>
    /// Profil étendu pour un Utilisateur avec rôle Entrepreneur.
    /// Relation 1-1 avec Utilisateur.
    /// </summary>
    public class Entrepreneur
    {
        [Key]
        public int IdEntrepreneur { get; set; }

        [Required]
        [StringLength(100)]
        public string Specialite { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Ville { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(30)]
        public string? NumeroMoMo { get; set; }

        [Range(0, 5)]
        public double ScoreFilabilite { get; set; } = 0;

        public int NbProjetsTermines { get; set; } = 0;
        public int NbRefusConsecutifs { get; set; } = 0;

        public string IdUtilisateur { get; set; } = string.Empty;

        [ForeignKey(nameof(IdUtilisateur))]
        public Utilisateur? Utilisateur { get; set; }

        public ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}