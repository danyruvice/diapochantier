using System.ComponentModel.DataAnnotations;
using DiaspoChantier.Enums;

namespace DiaspoChantier.Models
{
    public class InvitationProjet
    {
        [Key]
        public int IdInvitation { get; set; }

        public int IdProjet { get; set; }

        public int IdEntrepreneur { get; set; }

        public StatutInvitation Statut { get; set; } = StatutInvitation.EnAttente;

        [StringLength(500)]
        public string? Message { get; set; }

        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        public DateTime? DateReponse { get; set; }

        [StringLength(500)]
        public string? MotifRefus { get; set; }

        public Projet? Projet { get; set; }

        public Entrepreneur? Entrepreneur { get; set; }
    }
}
