using System.ComponentModel.DataAnnotations;

namespace DiaspoChantier.ViewModels;

public class ModeleCreationProjet
{
    [Required(ErrorMessage = "Le titre du projet est obligatoire.")]
    [StringLength(150, ErrorMessage = "Le titre ne doit pas dépasser 150 caractères.")]
    public string Titre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le type de projet est obligatoire.")]
    [StringLength(100, ErrorMessage = "Le type ne doit pas dépasser 100 caractères.")]
    public string TypeProjet { get; set; } = string.Empty;

    [Required(ErrorMessage = "La localisation est obligatoire.")]
    [StringLength(200, ErrorMessage = "La localisation ne doit pas dépasser 200 caractères.")]
    public string Localisation { get; set; } = string.Empty;

    [Range(-90, 90, ErrorMessage = "La latitude doit être comprise entre -90 et 90.")]
    public double Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "La longitude doit être comprise entre -180 et 180.")]
    public double Longitude { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Le budget total doit être supérieur à 0.")]
    public decimal BudgetTotal { get; set; }

    public List<ModeleJalonProjet> Jalons { get; set; } = new()
    {
        new ModeleJalonProjet(),
        new ModeleJalonProjet(),
        new ModeleJalonProjet()
    };
}

public class ModeleJalonProjet
{
    [Required(ErrorMessage = "Le titre du jalon est obligatoire.")]
    [StringLength(150, ErrorMessage = "Le titre du jalon ne doit pas dépasser 150 caractères.")]
    public string Titre { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "La description ne doit pas dépasser 500 caractères.")]
    public string? Description { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Le montant doit être supérieur ou égal à 0.")]
    public decimal Montant { get; set; }

    [DataType(DataType.Date)]
    public DateTime? DateLimite { get; set; }
}

public class TableauBordProjetViewModel
{
    public int TotalProjets { get; set; }
    public int ProjetsActifs { get; set; }
    public int JalonsEnAttente { get; set; }
    public List<ElementProjetViewModel> Projets { get; set; } = new();
}

public class ElementProjetViewModel
{
    public int IdProjet { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string TypeProjet { get; set; } = string.Empty;
    public string Localisation { get; set; } = string.Empty;
    public decimal BudgetTotal { get; set; }
    public string Statut { get; set; } = string.Empty;
    public DateTime DateCreation { get; set; }
    public int JalonsTotal { get; set; }
    public decimal TotalJalons { get; set; }
    public int JalonsValides { get; set; }
    public int JalonsEnAttente { get; set; }
}

public class DetailProjetViewModel
{
    public int IdProjet { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string TypeProjet { get; set; } = string.Empty;
    public string Localisation { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public decimal BudgetTotal { get; set; }
    public string Statut { get; set; } = string.Empty;
    public DateTime DateCreation { get; set; }
    public string? Entrepreneur { get; set; }
    public List<DetailJalonViewModel> Jalons { get; set; } = new();
    public List<PhotoProjetViewModel> Photos { get; set; } = new();
}

public class DetailJalonViewModel
{
    public int IdJalon { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Montant { get; set; }
    public int Ordre { get; set; }
    public DateTime? DateLimite { get; set; }
    public bool EstValide { get; set; }
    public DateTime? DateValidation { get; set; }
}

public class PhotoProjetViewModel
{
    public string Url { get; set; } = string.Empty;
    public string NomFichier { get; set; } = string.Empty;
    public string Horodatage { get; set; } = string.Empty;
    public string Latitude { get; set; } = string.Empty;
    public string Longitude { get; set; } = string.Empty;
    public string Distance { get; set; } = string.Empty;
    public bool EstAuthentique { get; set; }
    public string StatutTexte => EstAuthentique ? "Authentique" : "À vérifier";
    public string Source { get; set; } = "Caméra mobile";
    public string HashSHA256 { get; set; } = string.Empty;
}
