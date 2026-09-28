using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace MyApp.Namespace
{
    public class HomeController : Controller
    {
        // GET: HomeController
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions 
                { 
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    Path = "/"
                }
            );

            return LocalRedirect(returnUrl);
        }

    }
}
