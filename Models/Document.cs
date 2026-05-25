using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DiaspoChantier.Enums;

namespace DiaspoChantier.Models
{
    /// <summary>
    /// Document uploadé par un Entrepreneur (CNI, carte pro, portfolio).
    /// Stocké dans Azure Blob Storage — hashé SHA-256 pour intégrité.
    /// </summary>
    public class Document
    {
        [Key]
        public int IdDocument { get; set; }

        [Required]
        public TypeDocument Type { get; set; }

        /// <summary>URL Azure Blob Storage</summary>
        [Required]
        [StringLength(500)]
        public string UrlStockage { get; set; } = string.Empty;

        /// <summary>Nom original du fichier</summary>
        [StringLength(255)]
        public string NomFichier { get; set; } = string.Empty;

        /// <summary>Hash SHA-256 pour garantir l'intégrité</summary>
        [StringLength(64)]
        public string? HashSHA256 { get; set; }

        [StringLength(50)]
        public string? TypeMime { get; set; }

        public long TailleFichierOctets { get; set; }

        public DateTime DateUpload { get; set; } = DateTime.UtcNow;

        public StatutDocument Statut { get; set; } = StatutDocument.EnAttente;
        public string? CommentaireAdmin { get; set; }

        public int IdEntrepreneur { get; set; }

        [ForeignKey(nameof(IdEntrepreneur))]
        public Entrepreneur? Entrepreneur { get; set; }
    }
}