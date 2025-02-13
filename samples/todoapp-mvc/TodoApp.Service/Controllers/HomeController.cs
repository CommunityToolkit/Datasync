using Microsoft.AspNetCore.Mvc;

namespace TodoApp.Service.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
