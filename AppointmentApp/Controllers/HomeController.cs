using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace AppointmentApp.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            // Clear session variables when accessing the index
            Session["_profile"] = null;

            var email = Session["UserEmail"] as string;

            var REUESTEDBOOKINGS = "AppointmentReq" + email;
            Session[REUESTEDBOOKINGS] = null;

            var COMPLETEDBOOKINGS = "compAppointments" + email;

            Session[COMPLETEDBOOKINGS] = null;

            Session["contactId"] = null;


            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}