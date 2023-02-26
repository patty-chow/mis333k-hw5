using Microsoft.AspNetCore.Mvc;

namespace Chow_Patty_HW5.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
