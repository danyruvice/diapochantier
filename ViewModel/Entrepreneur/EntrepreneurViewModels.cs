namespace DiaspoChantier.ViewModels;

public class RechercheEntrepreneurViewModel
{
    public string? Recherche { get; set; }

    public List<EntrepreneurCarteViewModel> Entrepreneurs { get; set; } = new();

    public List<ProjetSelectionViewModel> Projets { get; set; } = new();

    public string? MessageSucces { get; set; }

    public string? MessageErreur { get; set; }
}

public class EntrepreneurCarteViewModel
{
    public int IdEntrepreneur { get; set; }

    public string NomComplet { get; set; } = string.Empty;

    public string Specialite { get; set; } = string.Empty;

    public string Ville { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? PhotoProfilUrl { get; set; }

    public double ScoreFilabilite { get; set; }

    public int NbProjetsTermines { get; set; }

    public int NbAvis { get; set; }

    public double NoteMoyenne { get; set; }

    public string? NumeroMoMo { get; set; }
}

public class ProjetSelectionViewModel
{
    public int IdProjet { get; set; }

    public string Titre { get; set; } = string.Empty;

    public string Statut { get; set; } = string.Empty;
}

public class EntrepreneurProfilViewModel
{
    public int IdEntrepreneur { get; set; }

    public string NomComplet { get; set; } = string.Empty;

    public string Specialite { get; set; } = string.Empty;

    public string Ville { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? PhotoProfilUrl { get; set; }

    public double ScoreFilabilite { get; set; }

    public int NbProjetsTermines { get; set; }

    public int NbAvis { get; set; }

    public double NoteMoyenne { get; set; }

    public string? NumeroMoMo { get; set; }

    public List<ProjetSelectionViewModel> Projets { get; set; } = new();

    public string? Message { get; set; }
}
