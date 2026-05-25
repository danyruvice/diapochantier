using DiaspoChantier.Data;
using DiaspoChantier.Enums;
using DiaspoChantier.Models;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DiaspoChantier.Services;

public sealed class PaiementService
{
    private readonly ApplicationDbContext _contexte;
    private readonly ServiceEmail _serviceEmail;
    private readonly ProjetService _projetService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaiementService> _logger;

    public PaiementService(ApplicationDbContext contexte, ServiceEmail serviceEmail, ProjetService projetService, IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<PaiementService> logger)
    {
        _contexte = contexte;
        _serviceEmail = serviceEmail;
        _projetService = projetService;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<InitierPaiementResult> InitierDepotAsync(string idUtilisateur, int idProjet, decimal montant, string canalTexte, string devise, string? telephone)
    {
        if (montant <= 0)
        {
            return InitierPaiementResult.Echec("Le montant doit être supérieur à 0.");
        }

        if (!Enum.TryParse<CanalPaiement>(canalTexte, true, out var canal))
        {
            return InitierPaiementResult.Echec("Canal de paiement invalide.");
        }

        var utilisateur = await _contexte.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == idUtilisateur);
        if (utilisateur is null)
        {
            return InitierPaiementResult.Echec("Utilisateur introuvable.");
        }

        var projet = await _contexte.Projets
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdProjet == idProjet && p.IdInvestisseur == idUtilisateur);

        if (projet is null)
        {
            return InitierPaiementResult.Echec("Projet introuvable ou vous n’êtes pas l’investisseur associé.");
        }

        if (canal == CanalPaiement.Stripe)
        {
            if (!string.Equals(devise, "cad", StringComparison.OrdinalIgnoreCase) && !string.Equals(devise, "eur", StringComparison.OrdinalIgnoreCase))
            {
                return InitierPaiementResult.Echec("Le dépôt Stripe accepte uniquement CAD ou EUR.");
            }

            var paymentIntent = await CreerPaymentIntentStripeAsync(utilisateur, projet, montant, devise);
            var paiement = new Paiement
            {
                IdProjet = projet.IdProjet,
                IdInvestisseur = utilisateur.Id,
                Montant = montant,
                Canal = CanalPaiement.Stripe,
                TypePaiement = TypePaiement.Depot,
                Statut = StatutPaiement.EnAttente,
                ReferenceExterne = paymentIntent.Id,
                Message = "Paiement Stripe en attente de confirmation.",
                DateCreation = DateTime.UtcNow,
                DateMiseAJour = DateTime.UtcNow
            };

            _contexte.Paiements.Add(paiement);
            await _contexte.SaveChangesAsync();

            return InitierPaiementResult.SuccesStripe(paymentIntent.ClientSecret, paymentIntent.Id, paiement.IdPaiement, paymentIntent.Id);
        }

        if (!string.Equals(devise, "xaf", StringComparison.OrdinalIgnoreCase))
        {
            return InitierPaiementResult.Echec("Le dépôt MTN MoMo accepte uniquement XAF.");
        }

        var numero = string.IsNullOrWhiteSpace(telephone)
            ? utilisateur.Telephone
            : telephone;

        if (string.IsNullOrWhiteSpace(numero))
        {
            return InitierPaiementResult.Echec("Renseignez un numéro MTN MoMo pour le dépôt mobile.");
        }

        var resultatMtn = await InitierMtnDepotAsync(utilisateur, projet, montant, numero);
        if (!resultatMtn.IsSuccess)
        {
            return InitierPaiementResult.Echec(resultatMtn.Message);
        }

        var paiementMtn = new Paiement
        {
            IdProjet = projet.IdProjet,
            IdInvestisseur = utilisateur.Id,
            Montant = montant,
            Canal = CanalPaiement.MtnMomo,
            TypePaiement = TypePaiement.Depot,
            Statut = StatutPaiement.EnAttente,
            ReferenceExterne = resultatMtn.ReferenceExterne,
            Message = resultatMtn.Message,
            DateCreation = DateTime.UtcNow,
            DateMiseAJour = DateTime.UtcNow
        };

        _contexte.Paiements.Add(paiementMtn);
        await _contexte.SaveChangesAsync();

        return InitierPaiementResult.SuccesMtn(resultatMtn.ReferenceExterne, paiementMtn.IdPaiement, resultatMtn.Message);
    }

    public async Task<ValidationPaiementResult> LibererPaiementPourJalonAsync(int idJalon)
    {
        var jalon = await _contexte.Jalons
            .Include(j => j.Projet)
            .FirstOrDefaultAsync(j => j.IdJalon == idJalon);

        if (jalon is null)
        {
            return ValidationPaiementResult.Echec("Jalon introuvable.");
        }

        if (jalon.EstValide)
        {
            return ValidationPaiementResult.Echec("Ce jalon est déjà validé.");
        }

        var projet = jalon.Projet;
        if (projet is null)
        {
            return ValidationPaiementResult.Echec("Le projet associé est introuvable.");
        }

        var hasAuthenticPhoto = await _contexte.Photos.AnyAsync(p => p.IdJalon == idJalon && p.EstAuthentique);
        if (!hasAuthenticPhoto)
        {
            return ValidationPaiementResult.Echec("La validation nécessite au moins une photo authentique du jalon.");
        }

        var entrepreneur = await _contexte.Entrepreneurs
            .Include(e => e.Utilisateur)
            .FirstOrDefaultAsync(e => e.IdEntrepreneur == projet.IdEntrepreneur);

        if (entrepreneur is null || string.IsNullOrWhiteSpace(entrepreneur.NumeroMoMo))
        {
            return ValidationPaiementResult.Echec("L’entrepreneur n’a pas de compte MTN MoMo configuré.");
        }

        var sequestre = await _contexte.Sequestres.FirstOrDefaultAsync(s => s.IdProjet == projet.IdProjet);
        if (sequestre is null || sequestre.MontantBloque < jalon.Montant)
        {
            return ValidationPaiementResult.Echec("Le séquestre ne contient pas assez de fonds pour libérer ce jalon.");
        }

        var resultatMtn = await LibererMtnPaiementAsync(entrepreneur, projet, jalon.Montant);
        if (!resultatMtn.IsSuccess)
        {
            return ValidationPaiementResult.Echec(resultatMtn.Message);
        }

        sequestre.MontantBloque -= jalon.Montant;
        sequestre.MontantLibere += jalon.Montant;
        sequestre.Statut = sequestre.MontantBloque > 0 ? StatutSequestre.Bloque : StatutSequestre.Libere;
        sequestre.DateMiseAJour = DateTime.UtcNow;

        var paiement = new Paiement
        {
            IdProjet = projet.IdProjet,
            IdInvestisseur = projet.IdInvestisseur,
            IdEntrepreneur = entrepreneur.IdEntrepreneur,
            IdSequestre = sequestre.IdSequestre,
            Montant = jalon.Montant,
            Canal = CanalPaiement.MtnMomo,
            TypePaiement = TypePaiement.Liberation,
            Statut = StatutPaiement.Confirme,
            ReferenceExterne = resultatMtn.ReferenceExterne,
            Message = $"Libération du jalon {jalon.Titre}.",
            DateCreation = DateTime.UtcNow,
            DateMiseAJour = DateTime.UtcNow,
            DatePaiement = DateTime.UtcNow
        };

        _contexte.Paiements.Add(paiement);
        jalon.EstValide = true;
        jalon.DateValidation = DateTime.UtcNow;
        await _contexte.SaveChangesAsync();
        await _projetService.MettreAJourCycleDeVieAsync(projet.IdProjet);

        if (!string.IsNullOrWhiteSpace(entrepreneur.Utilisateur?.Email))
        {
            await _serviceEmail.EnvoyerLiberationPaiementAsync(
                entrepreneur.Utilisateur.Email,
                entrepreneur.Utilisateur.Prenom,
                projet.Titre,
                jalon.Montant,
                resultatMtn.ReferenceExterne);
        }

        return ValidationPaiementResult.Succes(resultatMtn.ReferenceExterne, paiement.IdPaiement);
    }

    public async Task<WebhookPaiementResult> TraiterWebhookStripeAsync(string payload, string signature)
    {
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(payload, signature, _configuration["Payments:Stripe:WebhookSecret"]);
            var payloadHash = HashPayload(payload);

            if (await IsDuplicateWebhookAsync(stripeEvent.Id, stripeEvent.Type, payloadHash))
            {
                return WebhookPaiementResult.DejaTraite();
            }

            if (stripeEvent.Type == "payment_intent.succeeded")
            {
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                if (paymentIntent is null)
                {
                    return WebhookPaiementResult.Echec("Objet Stripe invalide.");
                }

                var paiement = await _contexte.Paiements.FirstOrDefaultAsync(p => p.ReferenceExterne == paymentIntent.Id);
                if (paiement is null)
                {
                    return WebhookPaiementResult.Echec("Aucun paiement lié à ce PaymentIntent.");
                }

                await ConfirmerDepotAsync(paiement, paymentIntent.Id, "Paiement Stripe confirmé par webhook.");
                return WebhookPaiementResult.Succes();
            }

            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Session;
                if (session is null)
                {
                    return WebhookPaiementResult.Echec("Session Stripe invalide.");
                }

                var paiement = await GetPaiementForStripeSessionAsync(session);
                if (paiement is null)
                {
                    return WebhookPaiementResult.Echec("Aucun paiement lié à cette session Stripe.");
                }

                await ConfirmerDepotAsync(paiement, session.Id, "Paiement Stripe confirmé via session checkout.");
                return WebhookPaiementResult.Succes();
            }

            return WebhookPaiementResult.Succes();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur webhook Stripe.");
            return WebhookPaiementResult.Echec("Erreur webhook Stripe.");
        }
    }

    public async Task<WebhookPaiementResult> TraiterWebhookMtnAsync(string payload, string secretHeader)
    {
        var expectedSecret = _configuration["Payments:MtnMomo:CallbackSecret"];
        if (!string.Equals(secretHeader, expectedSecret, StringComparison.Ordinal))
        {
            return WebhookPaiementResult.Echec("Secret MTN invalide.");
        }

        var payloadHash = HashPayload(payload);
        using var document = JsonDocument.Parse(payload);
        if (!document.RootElement.TryGetProperty("referenceId", out var referenceElement))
        {
            return WebhookPaiementResult.Echec("Référence MTN absente.");
        }

        var reference = referenceElement.GetString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(reference))
        {
            return WebhookPaiementResult.Echec("Référence MTN invalide.");
        }

        var status = document.RootElement.TryGetProperty("status", out var statusElement)
            ? statusElement.GetString()
            : null;

        var eventKey = $"MTN:{reference}:{status}";
        if (await IsDuplicateWebhookAsync(eventKey, "mtn.callback", payloadHash))
        {
            return WebhookPaiementResult.DejaTraite();
        }

        var paiement = await _contexte.Paiements.FirstOrDefaultAsync(p => p.ReferenceExterne == reference);
        if (paiement is null)
        {
            return WebhookPaiementResult.Echec("Aucun paiement MTN correspondant.");
        }

        if (string.Equals(status, "SUCCESSFUL", StringComparison.OrdinalIgnoreCase) || string.Equals(status, "SUCCESS", StringComparison.OrdinalIgnoreCase))
        {
            await ConfirmerDepotAsync(paiement, reference, "Paiement MTN confirmé par callback.");
            return WebhookPaiementResult.Succes();
        }

        paiement.Statut = StatutPaiement.Echoue;
        paiement.Message = status ?? "Paiement MTN en échec.";
        paiement.DateMiseAJour = DateTime.UtcNow;
        await _contexte.SaveChangesAsync();
        return WebhookPaiementResult.Succes();
    }

    private async Task<PaymentIntent> CreerPaymentIntentStripeAsync(Utilisateur utilisateur, Projet projet, decimal montant, string devise)
    {
        var secret = _configuration["Payments:Stripe:SecretKey"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException("La clé secrète Stripe n’est pas configurée.");
        }

        StripeConfiguration.ApiKey = secret;
        var service = new PaymentIntentService();
        var options = new PaymentIntentCreateOptions
        {
            Amount = (long)Math.Round(montant * 100m),
            Currency = devise.ToLowerInvariant(),
            Description = $"Dépôt séquestre - {projet.Titre}",
            ReceiptEmail = utilisateur.Email,
            PaymentMethodTypes = new List<string> { "card" },
            Metadata = new Dictionary<string, string>
            {
                ["projet_id"] = projet.IdProjet.ToString(),
                ["investisseur_id"] = utilisateur.Id,
                ["source"] = "diaspo-chantier"
            }
        };

        return await service.CreateAsync(options);
    }

    private async Task<MtnInitResult> InitierMtnDepotAsync(Utilisateur utilisateur, Projet projet, decimal montant, string telephone)
    {
        var baseUrl = _configuration["Payments:MtnMomo:BaseUrl"];
        var apiKey = _configuration["Payments:MtnMomo:ApiKey"];
        var subscriptionKey = _configuration["Payments:MtnMomo:SubscriptionKey"];
        var userId = _configuration["Payments:MtnMomo:UserId"];
        var callbackUrl = _configuration["Payments:MtnMomo:CallbackUrl"];

        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(subscriptionKey) || string.IsNullOrWhiteSpace(userId))
        {
            return MtnInitResult.Echec("La configuration MTN MoMo n’est pas complète.");
        }

        var reference = $"mtn-{Guid.NewGuid():N}";
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
        client.DefaultRequestHeaders.Add("X-Reference-Id", reference);
        client.DefaultRequestHeaders.Add("X-Target-Environment", "sandbox");
        client.DefaultRequestHeaders.Add("X-User-Id", userId);
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var payload = new
        {
            amount = (long)Math.Round(montant),
            currency = "XAF",
            externalId = reference,
            payer = new
            {
                partyIdType = "MSISDN",
                partyId = telephone
            },
            payerMessage = $"Dépôt séquestre pour {projet.Titre}",
            payeeNote = "Confirmation du dépôt Diaspo-Chantier"
        };

        var response = await client.PostAsJsonAsync(baseUrl.TrimEnd('/') + "/collection/v1_0/requesttopay", payload);
        if ((int)response.StatusCode is 200 or 202)
        {
            return MtnInitResult.Succes(reference, "Le dépôt MTN MoMo a été initié et attend la confirmation du callback.");
        }

        var body = await response.Content.ReadAsStringAsync();
        return MtnInitResult.Echec($"Échec MTN MoMo : {body}");
    }

    private async Task<MtnInitResult> LibererMtnPaiementAsync(Entrepreneur entrepreneur, Projet projet, decimal montant)
    {
        var baseUrl = _configuration["Payments:MtnMomo:BaseUrl"];
        var apiKey = _configuration["Payments:MtnMomo:ApiKey"];
        var subscriptionKey = _configuration["Payments:MtnMomo:SubscriptionKey"];
        var userId = _configuration["Payments:MtnMomo:UserId"];

        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(subscriptionKey) || string.IsNullOrWhiteSpace(userId))
        {
            return MtnInitResult.Echec("La configuration MTN MoMo n’est pas complète.");
        }

        var reference = $"mtn-liberation-{Guid.NewGuid():N}";
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
        client.DefaultRequestHeaders.Add("X-Reference-Id", reference);
        client.DefaultRequestHeaders.Add("X-Target-Environment", "sandbox");
        client.DefaultRequestHeaders.Add("X-User-Id", userId);
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var payload = new
        {
            amount = (long)Math.Round(montant),
            currency = "XAF",
            externalId = reference,
            payee = new
            {
                partyIdType = "MSISDN",
                partyId = entrepreneur.NumeroMoMo
            },
            payerMessage = $"Libération du jalon {projet.Titre}",
            payeeNote = "Paiement Diaspo-Chantier"
        };

        var response = await client.PostAsJsonAsync(baseUrl.TrimEnd('/') + "/disbursement/v1_0/transfer", payload);
        if ((int)response.StatusCode is 200 or 202)
        {
            return MtnInitResult.Succes(reference, "La libération MTN MoMo a été initiée.");
        }

        var body = await response.Content.ReadAsStringAsync();
        return MtnInitResult.Echec($"Échec libération MTN MoMo : {body}");
    }

    private async Task<Sequestre> GetOrCreateSequestreAsync(int idProjet, string idInvestisseur)
    {
        var sequestre = await _contexte.Sequestres.FirstOrDefaultAsync(s => s.IdProjet == idProjet && s.IdInvestisseur == idInvestisseur);
        if (sequestre is not null)
        {
            return sequestre;
        }

        sequestre = new Sequestre
        {
            IdProjet = idProjet,
            IdInvestisseur = idInvestisseur,
            MontantBloque = 0,
            MontantLibere = 0,
            Statut = StatutSequestre.Bloque,
            DateCreation = DateTime.UtcNow,
            DateMiseAJour = DateTime.UtcNow
        };

        _contexte.Sequestres.Add(sequestre);
        return sequestre;
    }

    private async Task ConfirmerDepotAsync(Paiement paiement, string reference, string message)
    {
        if (paiement.Statut == StatutPaiement.Confirme)
        {
            return;
        }

        paiement.Statut = StatutPaiement.Confirme;
        paiement.ReferenceExterne = reference;
        paiement.Message = message;
        paiement.DatePaiement = DateTime.UtcNow;
        paiement.DateMiseAJour = DateTime.UtcNow;

        var sequestre = await GetOrCreateSequestreAsync(paiement.IdProjet, paiement.IdInvestisseur);
        sequestre.MontantBloque += paiement.Montant;
        sequestre.Statut = StatutSequestre.Bloque;
        sequestre.DateMiseAJour = DateTime.UtcNow;

        if (!paiement.RecuEmailEnvoye)
        {
            var utilisateur = await _contexte.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == paiement.IdInvestisseur);
            if (utilisateur is not null)
            {
                await _serviceEmail.EnvoyerRecuDepotAsync(
                    utilisateur.Email ?? string.Empty,
                    utilisateur.Prenom,
                    paiement.Projet?.Titre ?? "Projet",
                    paiement.Montant,
                    paiement.Canal.ToString(),
                    reference);
            }

            paiement.RecuEmailEnvoye = true;
        }

        await _contexte.SaveChangesAsync();
    }

    private async Task<Paiement?> GetPaiementForStripeSessionAsync(Session session)
    {
        if (!session.Metadata.TryGetValue("paiement_id", out var paiementIdTexte) || !int.TryParse(paiementIdTexte, out var paiementId))
        {
            return await _contexte.Paiements.FirstOrDefaultAsync(p => p.ReferenceExterne == session.Id);
        }

        return await _contexte.Paiements.FindAsync(paiementId);
    }

    private async Task<bool> IsDuplicateWebhookAsync(string eventId, string eventType, string payloadHash)
    {
        var exists = await _contexte.WebhookEvents.AnyAsync(e => e.EventId == eventId);
        if (!exists)
        {
            _contexte.WebhookEvents.Add(new WebhookEvent
            {
                EventId = eventId,
                EventType = eventType,
                PayloadHash = payloadHash,
                ReceivedAt = DateTime.UtcNow
            });
            await _contexte.SaveChangesAsync();
            return false;
        }

        return true;
    }

    private static string HashPayload(string payload)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes);
    }

    private sealed class MtnInitResult
    {
        public bool IsSuccess { get; private init; }
        public string Message { get; private init; } = string.Empty;
        public string ReferenceExterne { get; private init; } = string.Empty;

        public static MtnInitResult Succes(string reference, string message) => new()
        {
            IsSuccess = true,
            ReferenceExterne = reference,
            Message = message
        };

        public static MtnInitResult Echec(string message) => new()
        {
            IsSuccess = false,
            Message = message
        };
    }
}

public sealed class InitierPaiementResult
{
    public bool IsSuccess { get; private init; }
    public string? Message { get; private init; }
    public string? ClientSecret { get; private init; }
    public string? PaymentIntentId { get; private init; }
    public string? ReferenceExterne { get; private init; }
    public int? PaiementId { get; private init; }

    public static InitierPaiementResult SuccesStripe(string clientSecret, string paymentIntentId, int paiementId, string reference) => new()
    {
        IsSuccess = true,
        ClientSecret = clientSecret,
        PaymentIntentId = paymentIntentId,
        PaiementId = paiementId,
        ReferenceExterne = reference,
        Message = "Paiement Stripe prêt à être confirmé."
    };

    public static InitierPaiementResult SuccesMtn(string reference, int paiementId, string message) => new()
    {
        IsSuccess = true,
        ReferenceExterne = reference,
        PaiementId = paiementId,
        Message = message
    };

    public static InitierPaiementResult Echec(string message) => new()
    {
        IsSuccess = false,
        Message = message
    };
}

public sealed class ValidationPaiementResult
{
    public bool IsSuccess { get; private init; }
    public string? Message { get; private init; }
    public string? ReferenceExterne { get; private init; }
    public int? PaiementId { get; private init; }

    public static ValidationPaiementResult Succes(string reference, int paiementId) => new()
    {
        IsSuccess = true,
        ReferenceExterne = reference,
        PaiementId = paiementId
    };

    public static ValidationPaiementResult Echec(string message) => new()
    {
        IsSuccess = false,
        Message = message
    };
}

public sealed class WebhookPaiementResult
{
    public bool IsSuccess { get; private init; }
    public bool IsAlreadyProcessed { get; private init; }
    public string? Message { get; private init; }

    public static WebhookPaiementResult Succes() => new() { IsSuccess = true };
    public static WebhookPaiementResult DejaTraite() => new() { IsAlreadyProcessed = true, IsSuccess = true };
    public static WebhookPaiementResult Echec(string message) => new() { IsSuccess = false, Message = message };
}
