using System.Net;
using System.Net.Mail;

namespace DiaspoChantier.Services
{
    public class ServiceEmail
    {
        private readonly IConfiguration _configuration;

        public ServiceEmail(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task EnvoyerAsync(string email, string sujet, string contenu)
        {
            return EnvoyerMessageAsync(email, sujet, contenu);
        }

        public Task EnvoyerLienReinitialisationAsync(string email, string urlReinitialisation)
        {
            var message = $"<p>Bonjour,</p><p>Pour réinitialiser votre mot de passe, cliquez sur ce lien :</p><p><a href='{urlReinitialisation}'>{urlReinitialisation}</a></p><p>Ce lien expirera dans 1 heure.</p>";
            return EnvoyerMessageAsync(email, "Réinitialisation du mot de passe", message);
        }

        public Task EnvoyerConfirmationInscriptionAsync(string email, string prenom)
        {
            var contenu = $"<p>Bonjour {prenom},</p><p>Votre compte Diaspo-Chantier a bien été créé.</p><p>Un administrateur pourra vérifier votre dossier selon le type de compte.</p>";
            return EnvoyerMessageAsync(email, "Confirmation de création de compte", contenu);
        }

        public Task EnvoyerDecisionVerificationAsync(string email, string nomComplet, string decision, string? motif)
        {
            var contenu = $"<p>Bonjour {nomComplet},</p><p>La vérification de votre dossier a été traitée avec la décision suivante : <strong>{decision}</strong>.</p>";
            if (!string.IsNullOrWhiteSpace(motif))
            {
                contenu += $"<p>Motif : {motif}</p>";
            }

            return EnvoyerMessageAsync(email, "Décision de vérification", contenu);
        }

        public Task EnvoyerRecuDepotAsync(string email, string prenom, string projet, decimal montant, string canal, string reference)
        {
            var contenu = $"<p>Bonjour {prenom},</p>" +
                $"<p>Votre dépôt de <strong>{montant:N0} FCFA</strong> pour le projet <strong>{projet}</strong> a bien été enregistré via {canal}.</p>" +
                $"<p>Référence : <strong>{reference}</strong></p>" +
                "<p>Les fonds sont actuellement bloqués dans le séquestre jusqu’à validation du jalon.</p>";

            return EnvoyerMessageAsync(email, "Reçu de dépôt - Diaspo-Chantier", contenu);
        }

        public Task EnvoyerLiberationPaiementAsync(string email, string prenom, string projet, decimal montant, string reference)
        {
            var contenu = $"<p>Bonjour {prenom},</p>" +
                $"<p>Le paiement de <strong>{montant:N0} FCFA</strong> pour le projet <strong>{projet}</strong> a été libéré.</p>" +
                $"<p>Référence : <strong>{reference}</strong></p>" +
                "<p>Vous pouvez consulter le détail dans votre tableau de bord.</p>";

            return EnvoyerMessageAsync(email, "Confirmation de paiement - Diaspo-Chantier", contenu);
        }

        private async Task EnvoyerMessageAsync(string email, string sujet, string contenu)
        {
            var hote = _configuration["Smtp:Host"];
            var portTexte = _configuration["Smtp:Port"];
            var expediteur = _configuration["Smtp:From"];
            var utilisateur = _configuration["Smtp:Username"];
            var motDePasse = _configuration["Smtp:Password"];
            var ssl = bool.TryParse(_configuration["Smtp:EnableSsl"], out var activerSsl) && activerSsl;

            if (string.IsNullOrWhiteSpace(hote))
            {
                throw new InvalidOperationException("La configuration SMTP est incomplète : Smtp:Host est vide.");
            }

            if (!int.TryParse(portTexte, out var port) || port <= 0)
            {
                throw new InvalidOperationException("La configuration SMTP est invalide : Smtp:Port doit être un nombre positif.");
            }

            if (string.IsNullOrWhiteSpace(expediteur))
            {
                throw new InvalidOperationException("La configuration SMTP est incomplète : Smtp:From est vide.");
            }

            if (string.IsNullOrWhiteSpace(utilisateur) || string.IsNullOrWhiteSpace(motDePasse))
            {
                throw new InvalidOperationException("La configuration SMTP est incomplète : les identifiants Smtp:Username et Smtp:Password sont requis.");
            }

            using var message = new MailMessage
            {
                From = new MailAddress(expediteur),
                Subject = sujet,
                Body = contenu,
                IsBodyHtml = true
            };
            message.To.Add(email);

            using var client = new SmtpClient(hote, port)
            {
                EnableSsl = ssl,
                Credentials = new NetworkCredential(utilisateur, motDePasse)
            };

            await client.SendMailAsync(message);
        }
    }
}