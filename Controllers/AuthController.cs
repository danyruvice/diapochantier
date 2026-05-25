using System.Security.Claims;
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
        private readonly SignInManager<Utilisateur> _signInManager;
        private readonly UserManager<Utilisateur> _userManager;

        public AuthController(
            AuthService serviceAuthentification,
            SignInManager<Utilisateur> signInManager,
            UserManager<Utilisateur> userManager)
        {
            _serviceAuthentification = serviceAuthentification;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Connexion()
        {
            return View("Login", new ConnexionViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Connexion(ConnexionViewModel modele)
        {
            if (!ModelState.IsValid)
            {
                return View("Login", modele);
            }

            var resultat = await _serviceAuthentification.ConnexionAsync(modele);
            if (resultat.Succeeded)
            {
                return RedirectToAction("Accueil", "Home");
            }

            ModelState.AddModelError(string.Empty, "Adresse email ou mot de passe incorrect.");
            return View("Login", modele);
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
        public IActionResult ChoisirTypeCompte()
        {
            return View("RegisterChoice");
        }

        [HttpGet]
        public IActionResult InscriptionInvestisseur()
        {
            return View("RegisterInvestisseur", new InscriptionInvestisseurViewModel());
        }

        [HttpPost]
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
            return RedirectToAction("Accueil", "Home");
        }

        [HttpGet]
        public IActionResult InscriptionEntrepreneur()
        {
            return View("RegisterEntrepreneur", new InscriptionEntrepreneurViewModel());
        }

        [HttpPost]
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
            return RedirectToAction("Accueil", "Home");
        }

        [HttpGet]
        public IActionResult MotDePasseOublie()
        {
            return View("ForgotPassword", new DemandeResetPasswordViewModel());
        }

        [HttpPost]
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
        public async Task<IActionResult> Deconnexion()
        {
            await _serviceAuthentification.DeconnexionAsync();
            return RedirectToAction("Accueil", "Home");
        }


        private string GetUrlRetour(string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return returnUrl;
            }

            return Url.Action("Accueil", "Home") ?? "/";
        }
    }
}