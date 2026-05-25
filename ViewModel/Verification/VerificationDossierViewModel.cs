using System.ComponentModel.DataAnnotations;

namespace DiaspoChantier.ViewModels
{
    public class VerificationDossierViewModel
    {
        public int IdEntrepreneur { get; set; }
        public string NomComplet { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Telephone { get; set; } = string.Empty;
        public string Ville { get; set; } = string.Empty;
        public string Specialite { get; set; } = string.Empty;
        public DateTime DateInscription { get; set; }
        public List<DocumentDto> Documents { get; set; } = new();
        public string StatutActuel { get; set; } = string.Empty;
    }

    public class DocumentDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string UrlStockage { get; set; } = string.Empty;
        public string NomFichier { get; set; } = string.Empty;
        public string Statut { get; set; } = string.Empty;
    }
}
