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
[Route("api/paiement")]
[Authorize]
public class PaiementApiController : ControllerBase
{
    private readonly PaiementService _paiementService;
    private readonly ApplicationDbContext _contexte;
    private readonly UserManager<Utilisateur> _users;

    public PaiementApiController(PaiementService paiementService, ApplicationDbContext contexte, UserManager<Utilisateur> users)
    {
        _paiementService = paiementService;
        _contexte = contexte;
        _users = users;
    }

    [HttpPost("initier")]
    [Authorize(Roles = nameof(RoleUtilisateur.Investisseur))]
    public async Task<IActionResult> Initier([FromBody] InitierPaiementApiRequest request)
    {
        var utilisateur = await _users.GetUserAsync(User);
        if (utilisateur is null)
        {
            return Unauthorized();
        }

        var resultat = await _paiementService.InitierDepotAsync(
            utilisateur.Id,
            request.ProjetId,
            request.Montant,
            request.Canal,
            request.Devise,
            request.Telephone);

        if (!resultat.IsSuccess)
        {
            return BadRequest(new { message = resultat.Message });
        }

        return Ok(resultat);
    }

    [HttpGet("{idUtilisateur}")]
    public async Task<IActionResult> Historique(string idUtilisateur)
    {
        var utilisateur = await _users.GetUserAsync(User);
        if (utilisateur is null || (utilisateur.Id != idUtilisateur && utilisateur.Role != RoleUtilisateur.Administrateur))
        {
            return Forbid();
        }

        var query = _contexte.Paiements.Include(p => p.Projet).AsQueryable();

        if (utilisateur.Role == RoleUtilisateur.Investisseur)
        {
            query = query.Where(p => p.IdInvestisseur == utilisateur.Id);
        }
        else if (utilisateur.Role == RoleUtilisateur.Entrepreneur)
        {
            var entrepreneur = await _contexte.Entrepreneurs.FirstOrDefaultAsync(e => e.IdUtilisateur == utilisateur.Id);
            if (entrepreneur is null)
            {
                return Forbid();
            }

            query = query.Where(p => p.IdEntrepreneur == entrepreneur.IdEntrepreneur);
        }

        var liste = await query.OrderByDescending(p => p.DateCreation).AsNoTracking().ToListAsync();
        var sequestres = await _contexte.Sequestres
            .Where(s => s.IdInvestisseur == utilisateur.Id)
            .AsNoTracking()
            .ToListAsync();

        return Ok(new
        {
            soldeBloque = sequestres.Sum(s => s.MontantBloque),
            soldeLibere = sequestres.Sum(s => s.MontantLibere),
            paiements = liste.Select(p => new
            {
                p.IdPaiement,
                p.Montant,
                canal = p.Canal.ToString(),
                statut = p.Statut.ToString(),
                type = p.TypePaiement.ToString(),
                p.DateCreation,
                projet = p.Projet?.Titre
            })
        });
    }
}

public class InitierPaiementApiRequest
{
    public int ProjetId { get; set; }
    public decimal Montant { get; set; }
    public string Canal { get; set; } = "Stripe";
    public string Devise { get; set; } = "EUR";
    public string? Telephone { get; set; }
}
