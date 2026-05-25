using DiaspoChantier.Enums;
using DiaspoChantier.Models;
using DiaspoChantier.Services;
using DiaspoChantier.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DiaspoChantier.Controllers
{
    public class AuthController : Controller
    {
        private readonly AuthService _serviceAuthentification;
        private readonly UserManager<Utilisateur> _userManager;

        public AuthController(
            AuthService serviceAuthentification,
            UserManager<Utilisateur> userManager)
        {
            _serviceAuthentification = serviceAuthentification;
            _userManager = userManager;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Connexion()
        {
            return View("Login", new ConnexionViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Connexion(ConnexionViewModel modele)
        {
            if (!ModelState.IsValid)
            {
                return View("Login", modele);
            }

            var resultat = await _serviceAuthentification.ConnexionAsync(modele);
            if (!resultat.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Adresse email ou mot de passe incorrect.");
                return View("Login", modele);
            }

            return await RedirigerVersTableauDeBordAsync();
        }

        [HttpPost("connexion-jwt")]
        [AllowAnonymous]
        public async Task<IActionResult> ConnexionJwt([FromBody] ConnexionViewModel modele)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var resultat = await _serviceAuthentification.ConnexionAsync(modele);
            if (!resultat.Succeeded)
            {
                return Unauthorized(new { message = "Adresse email ou mot de passe incorrect." });
            }

            var jeton = await _serviceAuthentification.GenererJetonConnexionAsync(modele.Email);
            if (string.IsNullOrWhiteSpace(jeton))
            {
                return Unauthorized(new { message = "Impossible de générer le jeton d'authentification." });
            }

            return Ok(new { jeton });
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ChoisirTypeCompte()
        {
            return View("RegisterChoice");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult InscriptionInvestisseur()
        {
            return View("RegisterInvestisseur", new InscriptionInvestisseurViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InscriptionInvestisseur(InscriptionInvestisseurViewModel modele)
        {
            if (!ModelState.IsValid)
            {
                return View("RegisterInvestisseur", modele);
            }

            var resultat = await _serviceAuthentification.CreerCompteInvestisseurAsync(modele);
            if (!resultat.Succeeded)
            {
                foreach (var erreur in resultat.Errors)
                {
                    ModelState.AddModelError(string.Empty, erreur.Description);
                }

                return View("RegisterInvestisseur", modele);
            }

            TempData["MessageReussite"] = "Votre compte investisseur a été créé avec succès.";
            return await RedirigerVersTableauDeBordAsync();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult InscriptionEntrepreneur()
        {
            return View("RegisterEntrepreneur", new InscriptionEntrepreneurViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InscriptionEntrepreneur(InscriptionEntrepreneurViewModel modele)
        {
            if (!ModelState.IsValid)
            {
                return View("RegisterEntrepreneur", modele);
            }

            var resultat = await _serviceAuthentification.CreerCompteEntrepreneurAsync(modele);
            if (!resultat.Succeeded)
            {
                foreach (var erreur in resultat.Errors)
                {
                    ModelState.AddModelError(string.Empty, erreur.Description);
                }

                return View("RegisterEntrepreneur", modele);
            }

            TempData["MessageReussite"] = "Votre compte entrepreneur a bien été créé et votre dossier est en attente de vérification.";
            return await RedirigerVersTableauDeBordAsync();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult MotDePasseOublie()
        {
            return View("ForgotPassword", new DemandeResetPasswordViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MotDePasseOublie(DemandeResetPasswordViewModel modele)
        {
            if (!ModelState.IsValid)
            {
                return View("ForgotPassword", modele);
            }

            var succes = await _serviceAuthentification.DemanderReinitialisationMotDePasseAsync(modele.Email);
            if (!succes)
            {
                ModelState.AddModelError(string.Empty, "Aucun compte ne correspond à cette adresse email.");
                return View("ForgotPassword", modele);
            }

            TempData["MessageReussite"] = "Si cet email existe, un lien de réinitialisation a été généré.";
            return RedirectToAction(nameof(Connexion));
        }

        [HttpGet("reinitialisation-mot-de-passe")]
        [AllowAnonymous]
        public IActionResult ReinitialisationMotDePasse([FromQuery] string email, [FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            {
                TempData["MessageErreur"] = "Le lien de réinitialisation est invalide ou expiré.";
                return RedirectToAction(nameof(MotDePasseOublie));
            }

            return View("ResetPassword", new ResetPasswordViewModel
            {
                Email = email,
                Token = token
            });
        }

        [HttpPost("reinitialisation-mot-de-passe")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReinitialisationMotDePasse(ResetPasswordViewModel modele)
        {
            if (!ModelState.IsValid)
            {
                return View("ResetPassword", modele);
            }

            var resultat = await _serviceAuthentification.ReinitialiserMotDePasseAsync(modele);
            if (!resultat.Succeeded)
            {
                foreach (var erreur in resultat.Errors)
                {
                    ModelState.AddModelError(string.Empty, erreur.Description);
                }

                return View("ResetPassword", modele);
            }

            TempData["MessageReussite"] = "Votre mot de passe a été mis à jour avec succès.";
            return RedirectToAction(nameof(Connexion));
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Deconnexion()
        {
            await _serviceAuthentification.DeconnexionAsync();
            return RedirectToAction("Accueil", "Home");
        }

        private async Task<IActionResult> RedirigerVersTableauDeBordAsync()
        {
            var utilisateur = await _userManager.GetUserAsync(User);
            if (utilisateur is null)
            {
                return RedirectToAction("Accueil", "Home");
            }

            if (utilisateur.Role == RoleUtilisateur.Administrateur)
            {
                return RedirectToAction("Index", "Admin");
            }

            return RedirectToAction("Index", "Dashboard");
        }
    }
}