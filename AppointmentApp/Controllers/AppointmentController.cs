using ClosedXML.Excel;
using CRMIntegration.CRMRepository;
using CRMIntegration.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Hosting;
using System.Web.Mvc;

namespace AppointmentApp.Controllers
{
    public class AppointmentController : ControllerExternal
    {

        public ActionResult List(string email, string filterType, string filterTypeForCompApp)
        {
            try
            {
                if (String.IsNullOrEmpty(email)) {

                    email = Session["UserEmail"] as string;
                }

                var userAppointmentReq = GetUserAppointmentReq(email, filterType);
                var completedAppointments = GetCompletedAppointments(email, filterTypeForCompApp);
                ViewBag.UserAppointmentRequests = userAppointmentReq;
                ViewBag.CompAppointments = completedAppointments;

                ViewBag.FeedBackCount = userAppointmentReq.Count();
                ViewBag.FeedBackCountForCompApp = completedAppointments.Count();

                ViewBag.SelectedFilter = filterType;
                ViewBag.SelectedAppFilter = filterTypeForCompApp;
            }
            catch(Exception ex)
            {
                TempData["error"] = "An error occurred while fetching your appointments. Please try again.";
                ViewBag.FeedBackCount = 0; 
            }

            return View();
        }


        public ActionResult Create()
        {
            return View();
        }

        public ActionResult FilterReqList(string FilterType, string FilterTypeForCompApp)
        {
            var Email = Session["UserEmail"] as string;

            if (!String.IsNullOrEmpty(FilterType) && !String.IsNullOrEmpty(FilterTypeForCompApp))
            {
                ViewBag.SelectedFilter = FilterType;
                ViewBag.SelectedAppFilter = FilterTypeForCompApp;
                return RedirectToAction("List", "Appointment", new { email = Email, filterType = FilterType, filterTypeForCompApp = FilterTypeForCompApp });
            }

            else if (String.IsNullOrEmpty(FilterTypeForCompApp) && !String.IsNullOrEmpty(FilterType))
            {
                ViewBag.SelectedFilter = FilterType;
                ViewBag.SelectedAppFilter = "all";
                return RedirectToAction("List", "Appointment", new { email = Email, filterType = FilterType, filterTypeForCompApp = "all" });
            } 

            else if (String.IsNullOrEmpty(FilterType) && !String.IsNullOrEmpty(FilterTypeForCompApp)) {
                ViewBag.SelectedFilter = "all";
                ViewBag.SelectedAppFilter = FilterTypeForCompApp;
                return RedirectToAction("List", "Appointment", new { email = Email, filterType = "all", filterTypeForCompApp = FilterTypeForCompApp });
            }
            else
            {
                ViewBag.SelectedFilter = "all";
                ViewBag.SelectedAppFilter = "all";
                return RedirectToAction("List", "Appointment", new { email = Email, filterType = "all", filterTypeForCompApp = "all" });
            }
            

        }


        [HttpPost]
        public ActionResult Create(CustomerAppointment model)
        {
            if (ModelState.IsValid)
            {
                var email = Session["UserEmail"] as string;
                string contactid = Session["contactId"] as string;

                

                if (contactid == null) {
                    // Read existing users
                    var userDataPath = HostingEnvironment.MapPath("~/userData/userData.json");
                    List<CreateUser> users = new List<CreateUser>();

                    if (System.IO.File.Exists(userDataPath))
                    {
                        var jsonData = System.IO.File.ReadAllText(userDataPath);
                        users = JsonConvert.DeserializeObject<List<CreateUser>>(jsonData) ?? new List<CreateUser>();
                    }

                    var userRecord = users.FirstOrDefault(u => u.emailAddress.Equals(email, StringComparison.OrdinalIgnoreCase));

                    if (userRecord != null)
                    {
                        contactid = userRecord.contactId;
                    }
                }




                var AppReq = new CustomerAppointment
                {
                    DiscussionSubject = model.DiscussionSubject,
                    Description = model.Description,
                    PreferredDays = model.PreferredDays,
                    PreferredTime = model.PreferredTime
                };

                ApiQuery crmApi = new ApiQuery();
                var info = crmApi.CreateAppointmentReq(AppReq, contactid);

                if (!string.IsNullOrEmpty(info.ToString()))
                {
                    var REUESTEDBOOKINGS = "AppointmentReq" + email;
                    Session[REUESTEDBOOKINGS] = null; //reset the cached booking to include the newly created booking

                    TempData["msg"] = "Request Created Successfully";

                    PopulateAlertViewBag();

                    return Json(new { success = true, redirectUrl = Url.Action("List", "Appointment", new { email = email, filterType = "", filterTypeForCompApp = "" }) });

                }
            }
                return View();
        }

        private List<AppointmentReqEntity> GetUserAppointmentReq(string Email, string FilterType)
        {
            var REUESTEDBOOKINGS = "AppointmentReq" + Email;
            List<AppointmentReqEntity> bookings = (List<AppointmentReqEntity>)Session[REUESTEDBOOKINGS];
            ApiQuery crmApi = new ApiQuery();
            DateTime currentDate = DateTime.Now;

            // Check if session is null, then query the API
            if (bookings == null)
            {
                var bookingReq = crmApi.getAppointmentReqByEmail(Email);

                if (bookingReq != null)
                {
                    Session[REUESTEDBOOKINGS] = bookingReq;
                    bookings = bookingReq;
                }
                else
                {
                    return new List<AppointmentReqEntity>(); // Return an empty list if no record is found
                }
            }

            if (!String.IsNullOrEmpty(FilterType))
            {
                if (FilterType == "this-week")
                {
                    DateTime startOfWeek = currentDate.AddDays(-(int)currentDate.DayOfWeek);
                    DateTime endOfWeek = startOfWeek.AddDays(6);
                    startOfWeek = startOfWeek.Date; // Set time to 00:00:00
                    endOfWeek = endOfWeek.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

                    return bookings.Where(e => e.DateCreated >= startOfWeek && e.DateCreated <= endOfWeek).ToList();
                }
                else if (FilterType == "previous-week")
                {
                    DateTime startOfPreviousWeek = currentDate.AddDays(-(int)currentDate.DayOfWeek - 7);
                    DateTime endOfPreviousWeek = startOfPreviousWeek.AddDays(6);

                    startOfPreviousWeek = startOfPreviousWeek.Date; // Set time to 00:00:00
                    endOfPreviousWeek = endOfPreviousWeek.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

                    return bookings.Where(e => e.DateCreated >= startOfPreviousWeek && e.DateCreated <= endOfPreviousWeek).ToList();
                }
                else if (FilterType == "this-month")
                {
                    DateTime startOfMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
                    DateTime endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                    startOfMonth = startOfMonth.Date; // Set time to 00:00:00
                    endOfMonth = endOfMonth.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

                    return bookings.Where(e => e.DateCreated >= startOfMonth && e.DateCreated <= endOfMonth).ToList();
                }
                else if (FilterType == "previous-month")
                {
                    DateTime startOfPreviousMonth = new DateTime(currentDate.Year, currentDate.Month, 1).AddMonths(-1);
                    DateTime endOfPreviousMonth = startOfPreviousMonth.AddMonths(1).AddDays(-1);

                    startOfPreviousMonth = startOfPreviousMonth.Date; // Set time to 00:00:00
                    endOfPreviousMonth = endOfPreviousMonth.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

                    return bookings.Where(e => e.DateCreated >= startOfPreviousMonth && e.DateCreated <= endOfPreviousMonth).ToList();
                }
                else if (FilterType == "all")
                {
                    return bookings;
                }
                else
                {
                    return bookings;
                }
            }           
            else
            {
                return bookings;
            }
        }

        private List<CompletedAppointmentsDto> GetCompletedAppointments(string Email, string FilterType)
        {
            var COMPLETEDBOOKINGS = "compAppointments" + Email;
            List<CompletedAppointmentsDto> bookingReports = (List<CompletedAppointmentsDto>)Session[COMPLETEDBOOKINGS];
            ApiQuery crmApi = new ApiQuery();
            DateTime currentDate = DateTime.Now;
            string contactid = Session["contactId"] as string;

            if(contactid == null)
            {
                var userDataPath = HostingEnvironment.MapPath("~/userData/userData.json");
                List<CreateUser> users = new List<CreateUser>();
                if (System.IO.File.Exists(userDataPath))
                {
                    var jsonData = System.IO.File.ReadAllText(userDataPath);
                    users = JsonConvert.DeserializeObject<List<CreateUser>>(jsonData) ?? new List<CreateUser>();
                }

                var userRecord = users.FirstOrDefault(u => u.emailAddress.Equals(Email, StringComparison.OrdinalIgnoreCase));


                if (userRecord != null)
                {
                    contactid = userRecord.contactId;
                }
            }


            // Check if session is null, then query the API

            if (bookingReports == null)
            {
                var compBooking = crmApi.getCompletedAppointments(contactid);

                if (compBooking != null)
                {
                    Session[COMPLETEDBOOKINGS] = compBooking;
                    bookingReports = compBooking;
                }
                else
                {
                    return new List<CompletedAppointmentsDto>(); // Return an empty list if no record is found
                }

            }

            if (!String.IsNullOrEmpty(FilterType))
            {
                if (FilterType == "current-week")
                {
                    DateTime startOfWeek = currentDate.AddDays(-(int)currentDate.DayOfWeek);
                    DateTime endOfWeek = startOfWeek.AddDays(6);
                    startOfWeek = startOfWeek.Date; // Set time to 00:00:00
                    endOfWeek = endOfWeek.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

                    return bookingReports.Where(e => e.startTime >= startOfWeek && e.startTime <= endOfWeek).ToList();
                }
                else if (FilterType == "previous-week")
                {
                    DateTime startOfPreviousWeek = currentDate.AddDays(-(int)currentDate.DayOfWeek - 7);
                    DateTime endOfPreviousWeek = startOfPreviousWeek.AddDays(6);

                    startOfPreviousWeek = startOfPreviousWeek.Date; // Set time to 00:00:00
                    endOfPreviousWeek = endOfPreviousWeek.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

                    return bookingReports.Where(e => e.startTime >= startOfPreviousWeek && e.startTime <= endOfPreviousWeek).ToList();
                }
                else if (FilterType == "next-week")
                {
                    DateTime startOfNextWeek = currentDate.AddDays(7 - (int)currentDate.DayOfWeek); // Start of next week
                    DateTime endOfNextWeek = startOfNextWeek.AddDays(6); // End of next week

                    startOfNextWeek = startOfNextWeek.Date; // Set time to 00:00:00
                    endOfNextWeek = endOfNextWeek.Date.AddHours(23).AddMinutes(59).AddSeconds(59); // Set time to end of the day

                    return bookingReports.Where(e => e.startTime >= startOfNextWeek && e.startTime <= endOfNextWeek).ToList();
                }
                else if (FilterType == "previous-month")
                {
                    DateTime startOfPreviousMonth = new DateTime(currentDate.Year, currentDate.Month, 1).AddMonths(-1);
                    DateTime endOfPreviousMonth = startOfPreviousMonth.AddMonths(1).AddDays(-1);

                    startOfPreviousMonth = startOfPreviousMonth.Date; // Set time to 00:00:00
                    endOfPreviousMonth = endOfPreviousMonth.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

                    return bookingReports.Where(e => e.startTime >= startOfPreviousMonth && e.startTime <= endOfPreviousMonth).ToList();
                }
                else if (FilterType == "all")
                {
                    return bookingReports;
                }
                else
                {
                    return bookingReports;
                }
            }
            else
            {
                return bookingReports;
            }
        }

        public ActionResult ExportList()
        {
            var email = Session["UserEmail"] as string;

            ApiQuery crmApi = new ApiQuery();

            

            var entity = crmApi.getAppointmentReqByEmail(email)
             .Select(e => new
             {
                 AppointmentSubject = e.DiscussionSubject,
                 Description = e.Description,
                 DateRequested = e.DateCreated.ToString("dd-MM-yy"),
                 PreferredDays = e.PreferredDays,
                 PreferredTime = e.PreferredTime,
                 Status = e.status,

             })
              .OrderByDescending(e => e.DateRequested).ToList();


            XLWorkbook wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("My Appointment Requests");
            ws.Cell(2, 1).InsertTable(entity);
            HttpContext.Response.Clear();
            HttpContext.Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            HttpContext.Response.AddHeader("content-disposition", String.Format(@"attachment;filename=AppointmentRequest-{0}.xlsx", DateTime.Now.ToString("ddMMhmmss")));

            using (MemoryStream memoryStream = new MemoryStream())
            {
                wb.SaveAs(memoryStream);
                memoryStream.WriteTo(HttpContext.Response.OutputStream);
                //memoryStream.Close();
            }

            HttpContext.Response.End();

            PopulateAlertViewBag();
            return View("List");
        }

        public ActionResult ExportCompAppList()
        {
            var contactId = Session["contactId"] as string;

            ApiQuery crmApi = new ApiQuery();



            var entity = crmApi.getCompletedAppointments(contactId)
             .Select(e => new
             {
                 Subject = e.subject,
                 StartTime = e.startTime.ToString("dd-MM-yy HH:mm"),
                 EndTime = e.endTime.ToString("dd-MM-yy HH:mm"),
                 ExecutiveName = e.executiveName,
                 RelationshipMgr = e.relMgerName,
                 Status = e.status,

             })
              .OrderByDescending(e => e.Status).ToList();


            XLWorkbook wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("My Completed Appointments");
            ws.Cell(2, 1).InsertTable(entity);
            HttpContext.Response.Clear();
            HttpContext.Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            HttpContext.Response.AddHeader("content-disposition", String.Format(@"attachment;filename=CompletedAppointments-{0}.xlsx", DateTime.Now.ToString("ddMMhmmss")));

            using (MemoryStream memoryStream = new MemoryStream())
            {
                wb.SaveAs(memoryStream);
                memoryStream.WriteTo(HttpContext.Response.OutputStream);
                //memoryStream.Close();
            }

            HttpContext.Response.End();

            PopulateAlertViewBag();
            return View("List");
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