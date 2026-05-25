using System.ComponentModel.DataAnnotations;

namespace DiaspoChantier.Models;

public class WebhookEvent
{
    [Key]
    public int IdWebhookEvent { get; set; }

    [Required]
    [StringLength(200)]
    public string EventId { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string EventType { get; set; } = string.Empty;

    [Required]
    [StringLength(64)]
    public string PayloadHash { get; set; } = string.Empty;

    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}
