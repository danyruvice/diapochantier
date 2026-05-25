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
            var utilisateur = await _gestionnaireUtilisateurs.GetUserAsync(User);
            if (utilisateur is null)
            {
                return RedirectToAction("Connexion", "Auth");
            }

            var modele = new GestionProfilViewModel
            {
                Nom = utilisateur.Nom,
                Prenom = utilisateur.Prenom,
                Email = utilisateur.Email ?? string.Empty,
                Telephone = utilisateur.Telephone,
                Adresse = utilisateur.Adresse,
                PhotoProfilUrlActuelle = utilisateur.PhotoProfilUrl,
                RoleAffiche = utilisateur.Role.ToString(),
                StatutAffiche = utilisateur.Statut.ToString(),
                Specialite = utilisateur.ProfilEntrepreneur?.Specialite,
                Ville = utilisateur.ProfilEntrepreneur?.Ville,
                NumeroMoMo = utilisateur.ProfilEntrepreneur?.NumeroMoMo
            };

            return View("Index", modele);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MettreAJourProfil(GestionProfilViewModel modele)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", modele);
            }

            var utilisateur = await _gestionnaireUtilisateurs.GetUserAsync(User);
            if (utilisateur is null)
            {
                return RedirectToAction("Connexion", "Auth");
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

                return View("Index", modele);
            }

            TempData["MessageReussite"] = "Votre profil a été mis à jour.";
            return RedirectToAction(nameof(MonProfil));
        }
    }
}