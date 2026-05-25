using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DiaspoChantier.Data;
using DiaspoChantier.Models;
using Microsoft.EntityFrameworkCore;

namespace DiaspoChantier.Services;

public class MateriauService
{
    private readonly ApplicationDbContext _contexte;
    private readonly ServiceStockage _stockage;
    private readonly IConfiguration _configuration;

    public MateriauService(ApplicationDbContext contexte, ServiceStockage stockage, IConfiguration configuration)
    {
        _contexte = contexte;
        _stockage = stockage;
        _configuration = configuration;
    }

    public async Task<Materiau?> EnregistrerAchatAsync(
        int idProjet,
        int idEntrepreneur,
        string fournisseur,
        string designation,
        decimal quantite,
        string unite,
        decimal prixTotal,
        IFormFile facture,
        string? hashAttendu = null)
    {
        var resultat = await _stockage.EnregistrerAsync(facture, $"projets/{idProjet}/factures", hashAttendu);

        var payload = new
        {
            idProjet,
            idEntrepreneur,
            fournisseur,
            designation,
            quantite,
            unite,
            prixTotal,
            resultat.HashSHA256,
            horodatage = DateTime.UtcNow
        };

        var qrPayload = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)));
        var baseUrl = _configuration["ApplicationBaseUrl"]?.TrimEnd('/') ?? "";
        var lienVerification = $"{baseUrl}/Materiau/Verifier?code={Uri.EscapeDataString(qrPayload)}";

        var materiau = new Materiau
        {
            IdProjet = idProjet,
            IdEntrepreneur = idEntrepreneur,
            Fournisseur = fournisseur.Trim(),
            Designation = designation.Trim(),
            Quantite = quantite,
            Unite = unite.Trim(),
            PrixTotal = prixTotal,
            UrlFacture = resultat.Url,
            HashSHA256 = resultat.HashSHA256,
            QrCodePayload = lienVerification
        };

        _contexte.Materiaux.Add(materiau);
        await _contexte.SaveChangesAsync();
        return materiau;
    }

    public async Task<(bool Valide, Materiau? Materiau)> VerifierQrAsync(string code)
    {
        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(code));
            var doc = JsonDocument.Parse(json);
            var hash = doc.RootElement.GetProperty("hashSHA256").GetString();

            var materiau = await _contexte.Materiaux
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.HashSHA256 == hash);

            if (materiau is null)
            {
                return (false, null);
            }

            return (string.Equals(materiau.HashSHA256, hash, StringComparison.OrdinalIgnoreCase), materiau);
        }
        catch
        {
            return (false, null);
        }
    }
}
