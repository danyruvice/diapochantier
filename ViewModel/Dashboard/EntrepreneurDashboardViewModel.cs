namespace DiaspoChantier.ViewModels;

public class EntrepreneurDashboardViewModel
{
    public string BonjourMessage { get; set; } = "Bonjour";
    public string? PhotoProfilUrl { get; set; }
    public string? Specialite { get; set; }
    public string? Ville { get; set; }
    public bool CompteVerifie { get; set; }
    public string? MoMo { get; set; }
    public double ScoreFiabilite { get; set; }
    public int ChantiersActifs { get; set; }
    public int JalonsASoumettre { get; set; }
    public decimal ARecevoir { get; set; }
    public List<DashboardProjetViewModel> Projets { get; set; } = new();
    public List<DashboardJalonViewModel> Jalons { get; set; } = new();
    public List<DashboardPhotoViewModel> Photos { get; set; } = new();
    public List<DashboardMateriauViewModel> Materiaux { get; set; } = new();
    public List<DashboardMessageViewModel> Messages { get; set; } = new();
}

public class DashboardProjetViewModel
{
    public int IdProjet { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string Investisseur { get; set; } = string.Empty;
    public int ProgressPourcentage { get; set; }
    public string Statut { get; set; } = string.Empty;
    public bool JalonsASoumettre { get; set; }
}

public class DashboardJalonViewModel
{
    public string ProjetTitre { get; set; } = string.Empty;
    public string Titre { get; set; } = string.Empty;
    public decimal Montant { get; set; }
    public DateTime? DateLimite { get; set; }
    public bool EstValide { get; set; }
    public string Statut => EstValide ? "Validé" : "À soumettre";
}

public class DashboardPhotoViewModel
{
    public int Index { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Horodatage { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool EstAuthentique { get; set; }
    public string StatutTexte => EstAuthentique ? "✓ Authentique" : "✗ Suspecte";
    public string Distance { get; set; } = string.Empty;
    public string Latitude { get; set; } = string.Empty;
    public string Longitude { get; set; } = string.Empty;
    public string HashSha256 { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
}

public class DashboardMateriauViewModel
{
    public string Designation { get; set; } = string.Empty;
    public string Quantite { get; set; } = string.Empty;
    public string Unite { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
}

public class DashboardMessageViewModel
{
    public string Auteur { get; set; } = string.Empty;
    public string Projet { get; set; } = string.Empty;
    public string Heure { get; set; } = string.Empty;
    public string Texte { get; set; } = string.Empty;
    public bool Lu { get; set; }
}
