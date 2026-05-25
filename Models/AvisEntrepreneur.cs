using System.ComponentModel.DataAnnotations;

namespace DiaspoChantier.Models;

public class AvisEntrepreneur
{
    [Key]
    public int IdAvis { get; set; }

    public int IdEntrepreneur { get; set; }

    public string IdInvestisseur { get; set; } = string.Empty;

    public int IdProjet { get; set; }

    [Range(1, 5)]
    public int Note { get; set; }

    [StringLength(500)]
    public string? Commentaire { get; set; }

    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    public Entrepreneur? Entrepreneur { get; set; }
}
