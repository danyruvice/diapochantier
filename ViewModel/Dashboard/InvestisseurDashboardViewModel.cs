namespace DiaspoChantier.ViewModels;

public class InvestisseurDashboardViewModel
{
    public string BonjourMessage { get; set; } = "Bonjour";

    public int ProjetsActifs { get; set; }

    public int JalonsEnAttente { get; set; }

    public decimal BudgetSousGestion { get; set; }

    public decimal TotalSequestre { get; set; }

    public int EntrepreneursActifs { get; set; }

    public List<ActiviteRecenteViewModel> ActiviteRecente { get; set; } = new();

    public List<DashboardProjetViewModel> Projets { get; set; } = new();

    public List<InvitationDashboardViewModel> Invitations { get; set; } = new();
}

public class InvitationDashboardViewModel
{
    public int IdInvitation { get; set; }

    public int IdProjet { get; set; }

    public string ProjetTitre { get; set; } = string.Empty;

    public string EntrepreneurNom { get; set; } = string.Empty;

    public string Statut { get; set; } = string.Empty;

    public string? Message { get; set; }

    public DateTime DateCreation { get; set; }
}

public class ActiviteRecenteViewModel
{
    public string Titre { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime Date { get; set; }
}
