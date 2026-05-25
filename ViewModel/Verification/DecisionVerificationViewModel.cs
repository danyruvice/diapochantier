using System.ComponentModel.DataAnnotations;

namespace DiaspoChantier.ViewModels
{
    public class DecisionVerificationViewModel
    {
        [Required]
        public int IdEntrepreneur { get; set; }

        [Required]
        public string Decision { get; set; } = string.Empty;

        [Display(Name = "Motif du rejet (obligatoire si rejet)")]
        public string? MotifRejet { get; set; }
    }
}
