using DiaspoChantier.Data;
using DiaspoChantier.Enums;
using DiaspoChantier.Models;
using Microsoft.EntityFrameworkCore;

namespace DiaspoChantier.Services;

public class ProjetService
{
    private readonly ApplicationDbContext _contexte;

    public ProjetService(ApplicationDbContext contexte)
    {
        _contexte = contexte;
    }

    public static int CalculerAvancementPourcentage(IEnumerable<Jalon> jalons)
    {
        var liste = jalons.ToList();
        if (liste.Count == 0)
        {
            return 0;
        }

        return (int)Math.Round((double)liste.Count(j => j.EstValide) / liste.Count * 100);
    }

    public async Task MettreAJourCycleDeVieAsync(int idProjet)
    {
        var projet = await _contexte.Projets
            .Include(p => p.Jalons)
            .FirstOrDefaultAsync(p => p.IdProjet == idProjet);

        if (projet is null || projet.Statut == StatutProjet.Annule)
        {
            return;
        }

        var jalons = projet.Jalons.ToList();
        if (jalons.Count > 0 && jalons.All(j => j.EstValide))
        {
            projet.Statut = StatutProjet.Termine;
            projet.DateMiseAJour = DateTime.UtcNow;
            await _contexte.SaveChangesAsync();
        }
    }

    public async Task<bool> PeutAnnulerProjetAsync(int idProjet)
    {
        var montantLibere = await _contexte.Paiements
            .Where(p => p.IdProjet == idProjet && p.TypePaiement == TypePaiement.Liberation && p.Statut == StatutPaiement.Confirme)
            .SumAsync(p => p.Montant);

        return montantLibere <= 0;
    }

    public async Task<AnnulationProjetResult> AnnulerProjetAsync(int idProjet)
    {
        var projet = await _contexte.Projets.FindAsync(idProjet);
        if (projet is null)
        {
            return AnnulationProjetResult.Echec("Projet introuvable.");
        }

        if (!await PeutAnnulerProjetAsync(idProjet))
        {
            return AnnulationProjetResult.Echec("Impossible d'annuler : des paiements ont déjà été libérés.");
        }

        projet.Statut = StatutProjet.Annule;
        projet.DateMiseAJour = DateTime.UtcNow;
        await _contexte.SaveChangesAsync();
        return AnnulationProjetResult.Succes();
    }
}

public sealed class AnnulationProjetResult
{
    public bool IsSuccess { get; init; }
    public string Message { get; init; } = string.Empty;

    public static AnnulationProjetResult Succes() => new() { IsSuccess = true };
    public static AnnulationProjetResult Echec(string message) => new() { IsSuccess = false, Message = message };
}
