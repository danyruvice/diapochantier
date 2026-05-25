using DiaspoChantier.Data;
using DiaspoChantier.Enums;
using DiaspoChantier.Models;
using DiaspoChantier.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DiaspoChantier.Services
{
    public class AuthService
    {
        private readonly UserManager<Utilisateur> _gestionnaireUtilisateurs;
        private readonly SignInManager<Utilisateur> _gestionnaireConnexion;
        private readonly RoleManager<IdentityRole> _gestionnaireRoles;
        private readonly ApplicationDbContext _contexte;
        private readonly ServiceStockage _serviceStockage;
        private readonly ServiceEmail _serviceEmail;
        private readonly ServiceJeton _serviceJeton;
        private readonly IConfiguration _configuration;

        public AuthService(
            UserManager<Utilisateur> gestionnaireUtilisateurs,
            SignInManager<Utilisateur> gestionnaireConnexion,
            RoleManager<IdentityRole> gestionnaireRoles,
            ApplicationDbContext contexte,
            ServiceStockage serviceStockage,
            ServiceEmail serviceEmail,
            ServiceJeton serviceJeton,
            IConfiguration configuration)
        {
            _gestionnaireUtilisateurs = gestionnaireUtilisateurs;
            _gestionnaireConnexion = gestionnaireConnexion;
            _gestionnaireRoles = gestionnaireRoles;
            _contexte = contexte;
            _serviceStockage = serviceStockage;
            _serviceEmail = serviceEmail;
            _serviceJeton = serviceJeton;
            _configuration = configuration;
        }

        public async Task<IdentityResult> CreerCompteInvestisseurAsync(InscriptionInvestisseurViewModel modele)
        {
            await AssurerRoleExisteAsync(RoleUtilisateur.Investisseur);

            var utilisateur = new Utilisateur
            {
                UserName = modele.Email,
                Email = modele.Email,
                Nom = modele.Nom,
                Prenom = modele.Prenom,
                Telephone = modele.Telephone,
                Adresse = modele.PaysResidence,
                Role = RoleUtilisateur.Investisseur,
                Statut = StatutCompte.AttenteVerification
            };

            var resultat = await _gestionnaireUtilisateurs.CreateAsync(utilisateur, modele.MotDePasse);
            if (!resultat.Succeeded)
            {
                return resultat;
            }

            await _gestionnaireUtilisateurs.AddToRoleAsync(utilisateur, RoleUtilisateur.Investisseur.ToString());
            await _serviceEmail.EnvoyerConfirmationInscriptionAsync(utilisateur.Email ?? modele.Email, utilisateur.Prenom);
            await _gestionnaireConnexion.SignInAsync(utilisateur, isPersistent: false);
            return resultat;
        }

        public async Task<IdentityResult> CreerCompteEntrepreneurAsync(InscriptionEntrepreneurViewModel modele)
        {
            await AssurerRoleExisteAsync(RoleUtilisateur.Entrepreneur);

            var utilisateur = new Utilisateur
            {
                UserName = modele.Email,
                Email = modele.Email,
                Nom = modele.Nom,
                Prenom = modele.Prenom,
                Telephone = modele.Telephone,
                Adresse = modele.Ville,
                Role = RoleUtilisateur.Entrepreneur,
                Statut = StatutCompte.AttenteVerification
            };

            var resultat = await _gestionnaireUtilisateurs.CreateAsync(utilisateur, modele.MotDePasse);
            if (!resultat.Succeeded)
            {
                return resultat;
            }

            await _gestionnaireUtilisateurs.AddToRoleAsync(utilisateur, RoleUtilisateur.Entrepreneur.ToString());

            var entrepreneur = new Entrepreneur
            {
                Specialite = modele.Specialite,
                Ville = modele.Ville,
                Description = modele.Description,
                NumeroMoMo = modele.NumeroMoMo,
                IdUtilisateur = utilisateur.Id,
                ScoreFilabilite = 0,
                NbProjetsTermines = 0,
                NbRefusConsecutifs = 0
            };

            await _contexte.AddAsync(entrepreneur);

            if (modele.FichierCNI is not null)
            {
                var resultatCni = await _serviceStockage.EnregistrerAsync(modele.FichierCNI, "documents");
                await _contexte.Documents.AddAsync(new Document
                {
                    Type = TypeDocument.CNI,
                    UrlStockage = resultatCni.Url,
                    NomFichier = modele.FichierCNI.FileName,
                    TypeMime = modele.FichierCNI.ContentType,
                    TailleFichierOctets = modele.FichierCNI.Length,
                    HashSHA256 = resultatCni.HashSHA256,
                    Statut = StatutDocument.EnAttente,
                    Entrepreneur = entrepreneur
                });
            }

            if (modele.FichierCartePro is not null)
            {
                var resultatCartePro = await _serviceStockage.EnregistrerAsync(modele.FichierCartePro, "documents");
                await _contexte.Documents.AddAsync(new Document
                {
                    Type = TypeDocument.CarteProfessionnelle,
                    UrlStockage = resultatCartePro.Url,
                    NomFichier = modele.FichierCartePro.FileName,
                    TypeMime = modele.FichierCartePro.ContentType,
                    TailleFichierOctets = modele.FichierCartePro.Length,
                    HashSHA256 = resultatCartePro.HashSHA256,
                    Statut = StatutDocument.EnAttente,
                    Entrepreneur = entrepreneur
                });
            }

            if (modele.FichierPortfolio is not null)
            {
                var resultatPortfolio = await _serviceStockage.EnregistrerAsync(modele.FichierPortfolio, "documents");
                await _contexte.Documents.AddAsync(new Document
                {
                    Type = TypeDocument.Portfolio,
                    UrlStockage = resultatPortfolio.Url,
                    NomFichier = modele.FichierPortfolio.FileName,
                    TypeMime = modele.FichierPortfolio.ContentType,
                    TailleFichierOctets = modele.FichierPortfolio.Length,
                    HashSHA256 = resultatPortfolio.HashSHA256,
                    Statut = StatutDocument.EnAttente,
                    Entrepreneur = entrepreneur
                });
            }

            await _contexte.SaveChangesAsync();
            await _serviceEmail.EnvoyerConfirmationInscriptionAsync(utilisateur.Email ?? modele.Email, utilisateur.Prenom);
            await _gestionnaireConnexion.SignInAsync(utilisateur, isPersistent: false);
            return resultat;
        }

        public async Task<SignInResult> ConnexionAsync(ConnexionViewModel modele)
        {
            return await _gestionnaireConnexion.PasswordSignInAsync(modele.Email, modele.MotDePasse, modele.SesouveniDeMoi, lockoutOnFailure: false);
        }

        public async Task<string?> GenererJetonConnexionAsync(string email)
        {
            var utilisateur = await _gestionnaireUtilisateurs.FindByEmailAsync(email);
            if (utilisateur is null)
            {
                return null;
            }

            var roles = await _gestionnaireUtilisateurs.GetRolesAsync(utilisateur);
            return _serviceJeton.GenererJeton(utilisateur, roles);
        }

        public async Task DeconnexionAsync()
        {
            await _gestionnaireConnexion.SignOutAsync();
        }

        public async Task<Utilisateur?> ObtenirUtilisateurActuelAsync(string identifiantUtilisateur)
        {
            return await _contexte.Users
                .Include(u => u.ProfilEntrepreneur)
                .FirstOrDefaultAsync(u => u.Id == identifiantUtilisateur);
        }

        public async Task<IdentityResult> MettreAJourProfilAsync(Utilisateur utilisateur, GestionProfilViewModel modele, string? urlPhoto = null)
        {
            utilisateur.Nom = modele.Nom;
            utilisateur.Prenom = modele.Prenom;
            utilisateur.Telephone = modele.Telephone;
            utilisateur.Adresse = modele.Adresse ?? utilisateur.Adresse;

            if (!string.IsNullOrWhiteSpace(urlPhoto))
            {
                utilisateur.PhotoProfilUrl = urlPhoto;
            }

            if (utilisateur.ProfilEntrepreneur is not null)
            {
                utilisateur.ProfilEntrepreneur.Specialite = modele.Specialite ?? utilisateur.ProfilEntrepreneur.Specialite;
                utilisateur.ProfilEntrepreneur.Ville = modele.Ville ?? utilisateur.ProfilEntrepreneur.Ville;
                utilisateur.ProfilEntrepreneur.NumeroMoMo = modele.NumeroMoMo ?? utilisateur.ProfilEntrepreneur.NumeroMoMo;
            }

            return await _gestionnaireUtilisateurs.UpdateAsync(utilisateur);
        }

        public async Task<bool> DemanderReinitialisationMotDePasseAsync(string email)
        {
            var utilisateur = await _gestionnaireUtilisateurs.FindByEmailAsync(email);
            if (utilisateur is null)
            {
                return false;
            }

            var jeton = await _gestionnaireUtilisateurs.GeneratePasswordResetTokenAsync(utilisateur);
            _contexte.TokensResetPassword.Add(new TokenResetPassword
            {
                Token = jeton,
                IdUtilisateur = utilisateur.Id,
                DateExpiration = DateTime.UtcNow.AddMinutes(60),
                DateCreation = DateTime.UtcNow
            });

            await _contexte.SaveChangesAsync();

            var baseUrl = _configuration["ApplicationBaseUrl"];
            var urlAbsolute = string.IsNullOrWhiteSpace(baseUrl)
                ? $"/Auth/ReinitialisationMotDePasse?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(jeton)}"
                : $"{baseUrl.TrimEnd('/')}/Auth/ReinitialisationMotDePasse?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(jeton)}";

            await _serviceEmail.EnvoyerLienReinitialisationAsync(email, urlAbsolute);
            return true;
        }

        public async Task<IdentityResult> ReinitialiserMotDePasseAsync(ResetPasswordViewModel modele)
        {
            var utilisateur = await _gestionnaireUtilisateurs.FindByEmailAsync(modele.Email);
            if (utilisateur is null)
            {
                return IdentityResult.Failed(new IdentityError { Code = "UtilisateurInconnu", Description = "Aucun compte ne correspond à cet email." });
            }

            var jetonValide = await _contexte.TokensResetPassword
                .FirstOrDefaultAsync(t => t.IdUtilisateur == utilisateur.Id && t.Token == modele.Token && !t.EstUtilise && t.DateExpiration > DateTime.UtcNow);

            if (jetonValide is null)
            {
                return IdentityResult.Failed(new IdentityError { Code = "TokenInvalide", Description = "Le token est invalide ou expiré." });
            }

            var resultat = await _gestionnaireUtilisateurs.ResetPasswordAsync(utilisateur, modele.Token, modele.NouveauMotDePasse);
            if (resultat.Succeeded)
            {
                jetonValide.EstUtilise = true;
                await _contexte.SaveChangesAsync();
            }

            return resultat;
        }

        public async Task<List<VerificationDossierViewModel>> RecupererEntrepreneursEnAttenteAsync()
        {
            var entrepreneurs = await _contexte.Entrepreneurs
                .Include(e => e.Utilisateur)
                .Include(e => e.Documents)
                .Where(e => e.Utilisateur != null && e.Utilisateur.Statut == StatutCompte.AttenteVerification)
                .ToListAsync();

            return entrepreneurs.Select(entrepreneur => new VerificationDossierViewModel
            {
                IdEntrepreneur = entrepreneur.IdEntrepreneur,
                NomComplet = $"{entrepreneur.Utilisateur?.Prenom} {entrepreneur.Utilisateur?.Nom}".Trim(),
                Email = entrepreneur.Utilisateur?.Email ?? string.Empty,
                Telephone = entrepreneur.Utilisateur?.Telephone ?? string.Empty,
                Ville = entrepreneur.Ville,
                Specialite = entrepreneur.Specialite,
                DateInscription = entrepreneur.Utilisateur?.DateInscription ?? DateTime.UtcNow,
                StatutActuel = entrepreneur.Utilisateur?.Statut.ToString() ?? StatutCompte.AttenteVerification.ToString(),
                Documents = entrepreneur.Documents.Select(document => new DocumentDto
                {
                    Id = document.IdDocument,
                    Type = document.Type.ToString(),
                    UrlStockage = document.UrlStockage,
                    NomFichier = document.NomFichier,
                    Statut = document.Statut.ToString()
                }).ToList()
            }).ToList();
        }

        public async Task<VerificationDossierViewModel?> RecupererDossierVerificationAsync(int idEntrepreneur)
        {
            var entrepreneur = await _contexte.Entrepreneurs
                .Include(e => e.Utilisateur)
                .Include(e => e.Documents)
                .FirstOrDefaultAsync(e => e.IdEntrepreneur == idEntrepreneur);

            if (entrepreneur is null || entrepreneur.Utilisateur is null)
            {
                return null;
            }

            return new VerificationDossierViewModel
            {
                IdEntrepreneur = entrepreneur.IdEntrepreneur,
                NomComplet = $"{entrepreneur.Utilisateur.Prenom} {entrepreneur.Utilisateur.Nom}".Trim(),
                Email = entrepreneur.Utilisateur.Email ?? string.Empty,
                Telephone = entrepreneur.Utilisateur.Telephone,
                Ville = entrepreneur.Ville,
                Specialite = entrepreneur.Specialite,
                DateInscription = entrepreneur.Utilisateur.DateInscription,
                StatutActuel = entrepreneur.Utilisateur.Statut.ToString(),
                Documents = entrepreneur.Documents.Select(document => new DocumentDto
                {
                    Id = document.IdDocument,
                    Type = document.Type.ToString(),
                    UrlStockage = document.UrlStockage,
                    NomFichier = document.NomFichier,
                    Statut = document.Statut.ToString()
                }).ToList()
            };
        }

        public async Task<bool> ChangerStatutVerificationAsync(int idEntrepreneur, StatutCompte statut, string? motif = null)
        {
            var entrepreneur = await _contexte.Entrepreneurs
                .Include(e => e.Utilisateur)
                .FirstOrDefaultAsync(e => e.IdEntrepreneur == idEntrepreneur);

            if (entrepreneur?.Utilisateur is null)
            {
                return false;
            }

            entrepreneur.Utilisateur.Statut = statut;
            await _contexte.SaveChangesAsync();

            await _serviceEmail.EnvoyerDecisionVerificationAsync(
                entrepreneur.Utilisateur.Email ?? string.Empty,
                $"{entrepreneur.Utilisateur.Prenom} {entrepreneur.Utilisateur.Nom}".Trim(),
                statut.ToString(),
                motif);

            return true;
        }

        private async Task AssurerRoleExisteAsync(RoleUtilisateur role)
        {
            var nomRole = role.ToString();
            if (!await _gestionnaireRoles.RoleExistsAsync(nomRole))
            {
                await _gestionnaireRoles.CreateAsync(new IdentityRole(nomRole));
            }
        }
    }
}