
using Microsoft.AspNetCore.Mvc;

namespace MvcChatSample.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index() => View();
    }
}
