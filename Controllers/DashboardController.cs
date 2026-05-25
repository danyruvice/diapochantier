using DiaspoChantier.Enums;
using DiaspoChantier.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DiaspoChantier.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly UserManager<Utilisateur> _userManager;

        public DashboardController(UserManager<Utilisateur> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var utilisateur = await _userManager.GetUserAsync(User);
            if (utilisateur?.Role == RoleUtilisateur.Administrateur)
            {
                return RedirectToAction("Index", "Admin");
            }

            return RedirectToAction("MonProfil", "Profil");
        }
    }
}