using System.ComponentModel.DataAnnotations;

namespace DiaspoChantier.Models
{
    /// <summary>
    /// Token sécurisé pour la réinitialisation de mot de passe.
    /// </summary>
    public class TokenResetPassword
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        public string IdUtilisateur { get; set; } = string.Empty;

        public DateTime DateExpiration { get; set; }

        public bool EstUtilise { get; set; } = false;

        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        public bool EstValide => !EstUtilise && DateExpiration > DateTime.UtcNow;
    }
}