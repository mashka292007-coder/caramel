using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CaramelCloud.UI.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        public IActionResult Index() => View();
    }
}
