using System.ComponentModel.DataAnnotations;

namespace DiaspoChantier.Models;

public class RefreshToken
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(450)]
    public string IdUtilisateur { get; set; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string Token { get; set; } = string.Empty;

    public DateTime DateExpiration { get; set; }

    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    public bool EstRevoque { get; set; }
}
