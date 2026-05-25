using DiaspoChantier.Enums;

namespace DiaspoChantier.ViewModels;

public class DepotPaiementViewModel
{
    public List<ElementProjetViewModel> Projets { get; set; } = new();
    public string? MessageSucces { get; set; }
    public string? MessageErreur { get; set; }
    public string? RedirectUrl { get; set; }
    public string? ClientSecret { get; set; }
    public string? PaymentIntentId { get; set; }
    public string? StatutPaiement { get; set; }
    public string? ReferenceExterne { get; set; }
    public decimal SoldeBloque { get; set; }
    public decimal SoldeLibere { get; set; }
}

public class HistoriquePaiementViewModel
{
    public decimal SoldeBloque { get; set; }
    public decimal SoldeLibere { get; set; }
    public List<DetailPaiementViewModel> Paiements { get; set; } = new();
}

public class DetailPaiementViewModel
{
    public int IdPaiement { get; set; }
    public string TitreProjet { get; set; } = string.Empty;
    public decimal Montant { get; set; }
    public string Canal { get; set; } = string.Empty;
    public string TypePaiement { get; set; } = string.Empty;
    public string Statut { get; set; } = string.Empty;
    public DateTime DateCreation { get; set; }
    public string Message { get; set; } = string.Empty;
}
