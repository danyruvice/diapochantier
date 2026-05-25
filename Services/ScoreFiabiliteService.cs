using DiaspoChantier.Data;
using DiaspoChantier.Enums;
using Microsoft.EntityFrameworkCore;

namespace DiaspoChantier.Services;

/// <summary>
/// Score pondéré : projets terminés (+), refus consécutifs (-), avis clients (+).
/// </summary>
public class ScoreFiabiliteService
{
    private readonly ApplicationDbContext _contexte;

    public ScoreFiabiliteService(ApplicationDbContext contexte)
    {
        _contexte = contexte;
    }

    public async Task RecalculerScoreAsync(int idEntrepreneur)
    {
        var entrepreneur = await _contexte.Entrepreneurs.FindAsync(idEntrepreneur);
        if (entrepreneur is null)
        {
            return;
        }

        var nbTermines = await _contexte.Projets.CountAsync(p =>
            p.IdEntrepreneur == idEntrepreneur && p.Statut == StatutProjet.Termine);

        var avis = await _contexte.AvisEntrepreneurs
            .Where(a => a.IdEntrepreneur == idEntrepreneur)
            .ToListAsync();

        var noteMoyenne = avis.Count == 0 ? 3.0 : avis.Average(a => a.Note);
        var penaliteRefus = Math.Min(entrepreneur.NbRefusConsecutifs * 0.3, 1.5);

        var score = (nbTermines * 0.4) + (noteMoyenne * 0.8) - penaliteRefus;
        entrepreneur.ScoreFilabilite = Math.Round(Math.Clamp(score, 0, 5), 2);
        entrepreneur.NbProjetsTermines = nbTermines;

        await _contexte.SaveChangesAsync();
    }
}
