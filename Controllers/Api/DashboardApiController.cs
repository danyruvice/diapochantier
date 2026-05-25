using DiaspoChantier.Data;
using DiaspoChantier.Enums;
using DiaspoChantier.Models;
using DiaspoChantier.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DiaspoChantier.Controllers.Api;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardApiController : ControllerBase
{
    private readonly ApplicationDbContext _contexte;
    private readonly UserManager<Utilisateur> _users;
    private readonly ScoreFiabiliteService _scoreService;

    public DashboardApiController(ApplicationDbContext contexte, UserManager<Utilisateur> users, ScoreFiabiliteService scoreService)
    {
        _contexte = contexte;
        _users = users;
        _scoreService = scoreService;
    }

    [HttpGet("investisseur")]
    [Authorize(Roles = nameof(RoleUtilisateur.Investisseur))]
    public async Task<IActionResult> Investisseur()
    {
        var utilisateur = await _users.GetUserAsync(User);
        if (utilisateur is null)
        {
            return Unauthorized();
        }

        var projets = await _contexte.Projets
            .Where(p => p.IdInvestisseur == utilisateur.Id)
            .Include(p => p.Jalons)
            .AsNoTracking()
            .ToListAsync();

        var sequestres = await _contexte.Sequestres
            .Where(s => s.IdInvestisseur == utilisateur.Id)
            .AsNoTracking()
            .ToListAsync();

        var actifs = projets.Where(p => p.Statut is not StatutProjet.Termine and not StatutProjet.Annule).ToList();

        return Ok(new
        {
            projetsActifs = actifs.Count,
            totalSequestre = sequestres.Sum(s => s.MontantBloque),
            jalonsAValider = projets.Sum(p => p.Jalons.Count(j => !j.EstValide)),
            projets = actifs.Select(p => new
            {
                p.IdProjet,
                p.Titre,
                p.Statut,
                avancementPourcent = ProjetService.CalculerAvancementPourcentage(p.Jalons)
            })
        });
    }

    [HttpGet("entrepreneur")]
    [Authorize(Roles = nameof(RoleUtilisateur.Entrepreneur))]
    public async Task<IActionResult> Entrepreneur()
    {
        var utilisateur = await _users.GetUserAsync(User);
        if (utilisateur is null)
        {
            return Unauthorized();
        }

        var profil = await _contexte.Entrepreneurs
            .FirstOrDefaultAsync(e => e.IdUtilisateur == utilisateur.Id);

        if (profil is null)
        {
            return NotFound();
        }

        await _scoreService.RecalculerScoreAsync(profil.IdEntrepreneur);

        var projets = await _contexte.Projets
            .Where(p => p.IdEntrepreneur == profil.IdEntrepreneur)
            .Include(p => p.Jalons)
            .AsNoTracking()
            .ToListAsync();

        var actifs = projets.Where(p => p.Statut is not StatutProjet.Termine and not StatutProjet.Annule).ToList();

        return Ok(new
        {
            chantiersActifs = actifs.Count,
            jalonsASoumettre = projets.Sum(p => p.Jalons.Count(j => !j.EstValide)),
            aRecevoir = projets.Sum(p => p.Jalons.Where(j => !j.EstValide).Sum(j => j.Montant)),
            scoreFiabilite = profil.ScoreFilabilite,
            nbAvis = await _contexte.AvisEntrepreneurs.CountAsync(a => a.IdEntrepreneur == profil.IdEntrepreneur),
            projets = actifs.Select(p => new
            {
                p.IdProjet,
                p.Titre,
                avancementPourcent = ProjetService.CalculerAvancementPourcentage(p.Jalons)
            })
        });
    }
}
