using Microsoft.AspNetCore.Mvc;

namespace SplitMate.Web.Controllers;

public class HelpController : Controller
{
    public IActionResult Index() => View();
}
