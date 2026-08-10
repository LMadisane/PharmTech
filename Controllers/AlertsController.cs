using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PharmTech.Controllers
{
    [Authorize(Roles = "Admin,Pharmacist")]
    public class AlertsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}