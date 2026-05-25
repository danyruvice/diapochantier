using DiaspoChantier.Enums;
using DiaspoChantier.Services;
using DiaspoChantier.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiaspoChantier.Controllers
{
    [Authorize(Roles = nameof(RoleUtilisateur.Administrateur))]
    public class AdminController : Controller
    {
        private readonly AuthService _serviceAuthentification;

        public AdminController(AuthService serviceAuthentification)
        {
            _serviceAuthentification = serviceAuthentification;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var dossiers = await _serviceAuthentification.RecupererEntrepreneursEnAttenteAsync();
            return View(dossiers);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var dossier = await _serviceAuthentification.RecupererDossierVerificationAsync(id);
            if (dossier is null)
            {
                return NotFound();
            }

            return View(dossier);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Decision(DecisionVerificationViewModel modele)
        {
            if (!ModelState.IsValid)
            {
                var dossier = await _serviceAuthentification.RecupererDossierVerificationAsync(modele.IdEntrepreneur);
                if (dossier is not null)
                {
                    return View("Details", dossier);
                }

                return RedirectToAction(nameof(Index));
            }

            if (!string.Equals(modele.Decision, "Approuver", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(modele.Decision, "Rejeter", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(modele.Decision), "La décision doit être Approuver ou Rejeter.");
                var dossier = await _serviceAuthentification.RecupererDossierVerificationAsync(modele.IdEntrepreneur);
                if (dossier is not null)
                {
                    return View("Details", dossier);
                }

                return RedirectToAction(nameof(Index));
            }

            if (string.Equals(modele.Decision, "Rejeter", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(modele.MotifRejet))
            {
                ModelState.AddModelError(nameof(modele.MotifRejet), "Le motif du rejet est obligatoire.");
                var dossier = await _serviceAuthentification.RecupererDossierVerificationAsync(modele.IdEntrepreneur);
                if (dossier is not null)
                {
                    return View("Details", dossier);
                }

                return RedirectToAction(nameof(Index));
            }

            var statut = string.Equals(modele.Decision, "Approuver", StringComparison.OrdinalIgnoreCase)
                ? StatutCompte.Actif
                : StatutCompte.Rejete;

            var succes = await _serviceAuthentification.ChangerStatutVerificationAsync(modele.IdEntrepreneur, statut, modele.MotifRejet);
            if (!succes)
            {
                TempData["MessageErreur"] = "Impossible de mettre à jour le dossier.";
                return RedirectToAction(nameof(Index));
            }

            TempData["MessageReussite"] = string.Equals(modele.Decision, "Approuver", StringComparison.OrdinalIgnoreCase)
                ? "Le dossier a été approuvé avec succès."
                : "Le dossier a été rejeté avec succès.";

            return RedirectToAction(nameof(Index));
        }
    }
}