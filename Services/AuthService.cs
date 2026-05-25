using System.Security.Cryptography;
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

        public async Task<IdentityResult> CreerCompteInvestisseurAsync(InscriptionInvestisseurViewModel modele, bool connexionAutomatique = true)
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

            if (modele.FichierPieceIdentite is not null)
            {
                var resultatPiece = await _serviceStockage.EnregistrerAsync(modele.FichierPieceIdentite, "investisseurs/identite");
                utilisateur.PieceIdentiteUrl = resultatPiece.Url;
                utilisateur.PieceIdentiteHashSha256 = resultatPiece.HashSHA256;
                await _gestionnaireUtilisateurs.UpdateAsync(utilisateur);
            }

            await _serviceEmail.EnvoyerConfirmationInscriptionAsync(utilisateur.Email ?? modele.Email, utilisateur.Prenom);
            if (connexionAutomatique)
            {
                await _gestionnaireConnexion.SignInAsync(utilisateur, isPersistent: false);
            }

            return resultat;
        }

        public async Task<IdentityResult> CreerCompteEntrepreneurAsync(InscriptionEntrepreneurViewModel modele)
        {
            await AssurerRoleExisteAsync(RoleUtilisateur.Entrepreneur);

            var existant = await _gestionnaireUtilisateurs.FindByEmailAsync(modele.Email);
            if (existant is not null)
            {
                if (existant.Statut == StatutCompte.Rejete && existant.DateDernierRejet.HasValue)
                {
                    var peutResoumettre = existant.DateDernierRejet.Value.AddDays(30) <= DateTime.UtcNow;
                    if (!peutResoumettre)
                    {
                        var joursRestants = (int)Math.Ceiling((existant.DateDernierRejet.Value.AddDays(30) - DateTime.UtcNow).TotalDays);
                        return IdentityResult.Failed(new IdentityError
                        {
                            Code = "RejetRecent",
                            Description = $"Vous pourrez soumettre un nouveau dossier dans {joursRestants} jour(s)."
                        });
                    }

                    return await ResoumettreDossierEntrepreneurAsync(existant, modele);
                }

                return IdentityResult.Failed(new IdentityError
                {
                    Code = "DuplicateEmail",
                    Description = "Cette adresse email est déjà utilisée."
                });
            }

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
            await AjouterDocumentsEntrepreneurAsync(entrepreneur, modele);
            await _contexte.SaveChangesAsync();
            await _serviceEmail.EnvoyerConfirmationInscriptionAsync(utilisateur.Email ?? modele.Email, utilisateur.Prenom);
            return resultat;
        }

        public async Task<ConnexionResultat> ConnexionAsync(ConnexionViewModel modele)
        {
            var utilisateur = await _gestionnaireUtilisateurs.FindByEmailAsync(modele.Email);
            if (utilisateur is null)
            {
                return ConnexionResultat.Echec("Adresse email ou mot de passe incorrect.");
            }

            if (utilisateur.Statut == StatutCompte.Suspendu)
            {
                return ConnexionResultat.Echec("Votre compte a été désactivé. Contactez le support Diaspo-Chantier.");
            }

            var resultat = await _gestionnaireConnexion.PasswordSignInAsync(modele.Email, modele.MotDePasse, modele.SesouveniDeMoi, lockoutOnFailure: false);
            if (!resultat.Succeeded)
            {
                return ConnexionResultat.Echec("Adresse email ou mot de passe incorrect.");
            }

            utilisateur.DerniereConnexion = DateTime.UtcNow;
            await _gestionnaireUtilisateurs.UpdateAsync(utilisateur);
            return ConnexionResultat.Succes(resultat);
        }

        public async Task<JetonConnexionResultat?> GenererJetonConnexionAsync(string email)
        {
            var utilisateur = await _gestionnaireUtilisateurs.FindByEmailAsync(email);
            if (utilisateur is null || utilisateur.Statut == StatutCompte.Suspendu)
            {
                return null;
            }

            var roles = await _gestionnaireUtilisateurs.GetRolesAsync(utilisateur);
            var accessToken = _serviceJeton.GenererJeton(utilisateur, roles);
            var refreshToken = await CreerRefreshTokenAsync(utilisateur.Id);

            return new JetonConnexionResultat
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpireMinutes = int.TryParse(_configuration["Jwt:ExpireMinutes"], out var minutes) ? minutes : 1440
            };
        }

        public async Task<JetonConnexionResultat?> RafraichirJetonAsync(string refreshToken)
        {
            var jeton = await _contexte.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == refreshToken && !t.EstRevoque && t.DateExpiration > DateTime.UtcNow);

            if (jeton is null)
            {
                return null;
            }

            var utilisateur = await _gestionnaireUtilisateurs.FindByIdAsync(jeton.IdUtilisateur);
            if (utilisateur is null || utilisateur.Statut == StatutCompte.Suspendu)
            {
                return null;
            }

            jeton.EstRevoque = true;
            var roles = await _gestionnaireUtilisateurs.GetRolesAsync(utilisateur);
            var accessToken = _serviceJeton.GenererJeton(utilisateur, roles);
            var nouveauRefresh = await CreerRefreshTokenAsync(utilisateur.Id);
            await _contexte.SaveChangesAsync();

            return new JetonConnexionResultat
            {
                AccessToken = accessToken,
                RefreshToken = nouveauRefresh,
                ExpireMinutes = int.TryParse(_configuration["Jwt:ExpireMinutes"], out var minutes) ? minutes : 1440
            };
        }

        public async Task DeconnexionAsync()
        {
            await _gestionnaireConnexion.SignOutAsync();
        }

        public async Task<Utilisateur?> ObtenirUtilisateurActuelAsync(string identifiantUtilisateur)
        {
            var utilisateur = await _contexte.Users
                .FirstOrDefaultAsync(u => u.Id == identifiantUtilisateur);

            if (utilisateur?.Role != RoleUtilisateur.Entrepreneur)
            {
                return utilisateur;
            }

            await _contexte.Entry(utilisateur)
                .Reference(u => u.ProfilEntrepreneur)
                .LoadAsync();

            return utilisateur;
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

            if (utilisateur.Role == RoleUtilisateur.Entrepreneur && utilisateur.ProfilEntrepreneur is not null)
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
            if (statut == StatutCompte.Rejete)
            {
                entrepreneur.Utilisateur.DateDernierRejet = DateTime.UtcNow;
            }

            await _contexte.SaveChangesAsync();

            await _serviceEmail.EnvoyerDecisionVerificationAsync(
                entrepreneur.Utilisateur.Email ?? string.Empty,
                $"{entrepreneur.Utilisateur.Prenom} {entrepreneur.Utilisateur.Nom}".Trim(),
                statut.ToString(),
                motif);

            return true;
        }

        private async Task<IdentityResult> ResoumettreDossierEntrepreneurAsync(Utilisateur utilisateur, InscriptionEntrepreneurViewModel modele)
        {
            utilisateur.Nom = modele.Nom;
            utilisateur.Prenom = modele.Prenom;
            utilisateur.Telephone = modele.Telephone;
            utilisateur.Adresse = modele.Ville;
            utilisateur.Statut = StatutCompte.AttenteVerification;
            utilisateur.DateDernierRejet = null;

            var resultatMdp = await _gestionnaireUtilisateurs.ResetPasswordAsync(
                utilisateur,
                await _gestionnaireUtilisateurs.GeneratePasswordResetTokenAsync(utilisateur),
                modele.MotDePasse);

            if (!resultatMdp.Succeeded)
            {
                return resultatMdp;
            }

            var entrepreneur = await _contexte.Entrepreneurs
                .Include(e => e.Documents)
                .FirstOrDefaultAsync(e => e.IdUtilisateur == utilisateur.Id);

            if (entrepreneur is null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "Profil entrepreneur introuvable." });
            }

            entrepreneur.Specialite = modele.Specialite;
            entrepreneur.Ville = modele.Ville;
            entrepreneur.Description = modele.Description;
            entrepreneur.NumeroMoMo = modele.NumeroMoMo;

            _contexte.Documents.RemoveRange(entrepreneur.Documents);
            await AjouterDocumentsEntrepreneurAsync(entrepreneur, modele);
            await _gestionnaireUtilisateurs.UpdateAsync(utilisateur);
            await _contexte.SaveChangesAsync();
            await _serviceEmail.EnvoyerConfirmationInscriptionAsync(utilisateur.Email ?? modele.Email, utilisateur.Prenom);

            return IdentityResult.Success;
        }

        private async Task AjouterDocumentsEntrepreneurAsync(Entrepreneur entrepreneur, InscriptionEntrepreneurViewModel modele)
        {
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
        }

        private async Task<string> CreerRefreshTokenAsync(string idUtilisateur)
        {
            var jours = int.TryParse(_configuration["Jwt:RefreshExpireDays"], out var d) ? d : 7;
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            _contexte.RefreshTokens.Add(new RefreshToken
            {
                IdUtilisateur = idUtilisateur,
                Token = token,
                DateExpiration = DateTime.UtcNow.AddDays(jours),
                DateCreation = DateTime.UtcNow
            });

            await _contexte.SaveChangesAsync();
            return token;
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

    public sealed class ConnexionResultat
    {
        public bool Succeeded { get; init; }
        public string? MessageErreur { get; init; }
        public SignInResult? SignInResult { get; init; }

        public static ConnexionResultat Succes(SignInResult resultat) => new() { Succeeded = true, SignInResult = resultat };
        public static ConnexionResultat Echec(string message) => new() { Succeeded = false, MessageErreur = message };
    }

    public sealed class JetonConnexionResultat
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public int ExpireMinutes { get; set; }
    }
}