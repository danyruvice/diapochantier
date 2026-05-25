using DiaspoChantier.Enums;
using DiaspoChantier.Models;
using DiaspoChantier.Services;
using DiaspoChantier.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DiaspoChantier.Controllers
{
    [Authorize]
    public class ProfilController : Controller
    {
        private readonly AuthService _serviceAuthentification;
        private readonly UserManager<Utilisateur> _gestionnaireUtilisateurs;
        private readonly ServiceStockage _serviceStockage;

        public ProfilController(AuthService serviceAuthentification, UserManager<Utilisateur> gestionnaireUtilisateurs, ServiceStockage serviceStockage)
        {
            _serviceAuthentification = serviceAuthentification;
            _gestionnaireUtilisateurs = gestionnaireUtilisateurs;
            _serviceStockage = serviceStockage;
        }

        [HttpGet]
        public async Task<IActionResult> MonProfil()
        {
            var identifiant = _gestionnaireUtilisateurs.GetUserId(User);
            if (string.IsNullOrWhiteSpace(identifiant))
            {
                return RedirectToAction("Connexion", "Auth");
            }

            var utilisateur = await _serviceAuthentification.ObtenirUtilisateurActuelAsync(identifiant);
            if (utilisateur is null)
            {
                return RedirectToAction("Connexion", "Auth");
            }

            return View("Index", ConstruireModeleProfil(utilisateur));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MettreAJourProfil(GestionProfilViewModel modele)
        {
            var identifiant = _gestionnaireUtilisateurs.GetUserId(User);
            if (string.IsNullOrWhiteSpace(identifiant))
            {
                return RedirectToAction("Connexion", "Auth");
            }

            var utilisateur = await _serviceAuthentification.ObtenirUtilisateurActuelAsync(identifiant);
            if (utilisateur is null)
            {
                return RedirectToAction("Connexion", "Auth");
            }

            if (!ModelState.IsValid)
            {
                return View("Index", ConstruireModeleProfil(utilisateur, modele));
            }

            string? urlPhoto = utilisateur.PhotoProfilUrl;
            if (modele.PhotoProfil is not null)
            {
                var resultatUpload = await _serviceStockage.EnregistrerAsync(modele.PhotoProfil, "profils");
                urlPhoto = resultatUpload.Url;
            }

            var resultat = await _serviceAuthentification.MettreAJourProfilAsync(utilisateur, modele, urlPhoto);
            if (!resultat.Succeeded)
            {
                foreach (var erreur in resultat.Errors)
                {
                    ModelState.AddModelError(string.Empty, erreur.Description);
                }

                return View("Index", ConstruireModeleProfil(utilisateur, modele));
            }

            TempData["MessageReussite"] = "Votre profil a été mis à jour.";
            return RedirectToAction(nameof(MonProfil));
        }

        private static GestionProfilViewModel ConstruireModeleProfil(Utilisateur utilisateur, GestionProfilViewModel? source = null)
        {
            var isEntrepreneur = utilisateur.Role == RoleUtilisateur.Entrepreneur;

            return new GestionProfilViewModel
            {
                Prenom = source?.Prenom ?? utilisateur.Prenom,
                Nom = source?.Nom ?? utilisateur.Nom,
                Email = utilisateur.Email ?? string.Empty,
                Telephone = source?.Telephone ?? utilisateur.Telephone,
                Adresse = source?.Adresse ?? utilisateur.Adresse,
                PhotoProfilUrlActuelle = utilisateur.PhotoProfilUrl,
                IsEntrepreneur = isEntrepreneur,
                RoleAffiche = utilisateur.Role.ToString(),
                StatutAffiche = utilisateur.Statut.ToString(),
                Specialite = isEntrepreneur ? (source?.Specialite ?? utilisateur.ProfilEntrepreneur?.Specialite) : null,
                Ville = isEntrepreneur ? (source?.Ville ?? utilisateur.ProfilEntrepreneur?.Ville) : null,
                NumeroMoMo = isEntrepreneur ? (source?.NumeroMoMo ?? utilisateur.ProfilEntrepreneur?.NumeroMoMo) : null
            };
        }
    }
}