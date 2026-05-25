using System.Diagnostics;
using DiaspoChantier.Models;
using Microsoft.AspNetCore.Mvc;

namespace DiaspoChantier.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return Accueil();
        }

        public IActionResult Accueil()
        {
            return View("Index");
        }

        public IActionResult CommentCaMarche()
        {
            return Redirect("/Home/Index#avantages");
        }

        public IActionResult Support()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return Confidentialite();
        }

        public IActionResult Confidentialite()
        {
            return View("Privacy");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
