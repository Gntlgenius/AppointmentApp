using System;
using System.Web.Mvc;

namespace AppointmentApp.Controllers
{
    public class ControllerExternal : Controller
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            try
            {
                string url = this.Request.Url.ToString().ToLower();
                // Allow access to login and register pages
                if (url.Contains("/appointment/list") || url.Contains("/appointment/create") || url.Contains("/appointment/exportlist"))
                {
                    // Check if the user is logged in
                    if (this.Session == null || this.Session["_profile"] == null)
                    {
                        // Redirect to the login page if not logged in
                        UrlHelper urlHelper = new UrlHelper(filterContext.RequestContext);
                        string loginUrl = urlHelper.Action("Index", "Home");
                        filterContext.Result = new RedirectResult(loginUrl);
                    }
                }

            }
            catch (Exception ex)
            {
                throw new Exception("Please Log in to Continue");
            }

            base.OnActionExecuting(filterContext);
        }
    }
}
