using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiaspoChantier.Models;

public class Photo
{
    [Key]
    public int IdPhoto { get; set; }

    [Required]
    public int IdProjet { get; set; }

    [Required]
    public int IdJalon { get; set; }

    [Required]
    [StringLength(500)]
    public string Url { get; set; } = string.Empty;

    [Required]
    [StringLength(64)]
    public string HashSHA256 { get; set; } = string.Empty;

    [Required]
    public double Latitude { get; set; }

    [Required]
    public double Longitude { get; set; }

    public double? Altitude { get; set; }

    [Required]
    public DateTime HorodatageGps { get; set; }

    [Required]
    public bool EstAuthentique { get; set; }

    [StringLength(100)]
    public string Source { get; set; } = "Caméra mobile";

    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    public bool AlerteEnvoyee { get; set; }

    [ForeignKey(nameof(IdProjet))]
    public Projet? Projet { get; set; }

    [ForeignKey(nameof(IdJalon))]
    public Jalon? Jalon { get; set; }
}
