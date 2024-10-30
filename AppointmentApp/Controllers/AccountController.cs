using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Linq;
using System.Web;
using System.IO;
using Newtonsoft.Json;
using System.Web.Hosting;
using CRMIntegration.Models;
using CRMIntegration.ConnectionMaster;
using CRMIntegration.CRMRepository;
using DocumentFormat.OpenXml.Spreadsheet;
using ClosedXML;

namespace AppointmentApp.Controllers
{
    public class AccountController : ControllerExternal
    {

        // Method to load user data from JSON file
        private List<CreateUser> LoadUserData()
        {
            var jsonFilePath = HostingEnvironment.MapPath("~/userData/userData.json");
            var jsonData = System.IO.File.ReadAllText(jsonFilePath);
            return JsonConvert.DeserializeObject<List<CreateUser>>(jsonData);
        }
    
        // POST: Account/Login
        [HttpPost]
        public ActionResult Login(string email, string password)
        {
            var users = LoadUserData();
            var firstName = string.Empty;

            // Validate user credentials
            var user = users.Find(u => u.emailAddress == email && u.password == password);
            if (user != null)
            {
                // Successful login, store the Profile, email and contact ID
                Session["_profile"] = user;

                CreateUser profile = Session["_profile"] as CreateUser;

                Session["contactId"] = profile.contactId.ToString();
                
                Session["UserEmail"] = email;

                PopulateAlertViewBag();

                return RedirectToAction("List", "Appointment", new { email = email });
            }
            else
            {
                ModelState.AddModelError("", "Invalid email or password. Please try again.");
                return View("Login"); // Show the Login view again with the error
            }
        }

        public ActionResult Back()
        {
            var email = Session["UserEmail"];

            if (email != null)
            {
                return RedirectToAction("List", "Appointment", new { email = email, filterType = "" });
            }
            else
            {
                return View(); 
            }
        }

        // GET: Account/Login
        public ActionResult Login()
        {
            return View();
        }

        public ActionResult Register()
        {
            return View();
            //return RedirectToAction("Register", "Home");
        }



        public ActionResult Create()
        {


            return View();
        }

        [HttpPost]
        public ActionResult Create(UserDetails model)
        {
            if (ModelState.IsValid)
            {

                var User = new CreateUser
                {
                    password = model.password,
                    firstName = model.firstName,
                    lastName = model.lastName,
                    middleName = model.middleName,
                    mobileNumber = model.mobileNumber,
                    companyName = model.companyName,
                    companyId = model.companyId,
                    emailAddress = model.emailAddress,
                    address = model.address
                };

                // Read existing users
                var userDataPath = HostingEnvironment.MapPath("~/userData/userData.json");
                List<CreateUser> users = new List<CreateUser>();



                if (System.IO.File.Exists(userDataPath))
                {
                    var jsonData = System.IO.File.ReadAllText(userDataPath);
                    users = JsonConvert.DeserializeObject<List<CreateUser>>(jsonData) ?? new List<CreateUser>();
                }

                // Check for duplicate user by email or lastname
                if (users.Any(u => u.emailAddress.Equals(User.emailAddress, StringComparison.OrdinalIgnoreCase) && u.lastName.Equals(User.lastName, StringComparison.OrdinalIgnoreCase)))
                {
                    ModelState.AddModelError("", "User with the same Email and lastname already Exists.");
                    return View("Create");
                }
                else
                {
                    ApiQuery crmApi = new ApiQuery();
                    var info = crmApi.CreateContact(User);
                    if (info != null)
                    {
                        User.contactId = info.ToString();
                        users.Add(User);
                        // Save back to the JSON file
                        var updatedJsonData = JsonConvert.SerializeObject(users, Formatting.Indented);
                        System.IO.File.WriteAllText(userDataPath, updatedJsonData);

                        return Json(new { success = true });

                    }
                }


            }
            else
            {
                // If there are errors, return the errors


                var error = "Error creating user in CRM.";
                return Json(new { success = false, error });
            }
            
            return View(model);
        }

        protected void PopulateAlertViewBag()
        {
            //This shows error/success message from other caller views
            if (TempData["errmsg"] != null || TempData["error"] != null)
                ViewBag.Error = TempData["errmsg"] != null ? TempData["errmsg"].ToString() : TempData["error"].ToString();

            if (TempData["warning"] != null)
                ViewBag.Warning = TempData["warning"].ToString();

            if (TempData["msg"] != null)
                ViewBag.Success = TempData["msg"].ToString();

        }

    }


}




