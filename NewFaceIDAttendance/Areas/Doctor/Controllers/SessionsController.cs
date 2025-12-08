using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NewFaceIDAttendance.Data;
using NewFaceIDAttendance.Models;
using NewFaceIDAttendance.ViewModels;

namespace NewFaceIDAttendance.Areas.Doctor.Controllers
{
    [Area("Doctor")]
    public class SessionsController : Controller
    {
        private readonly AppDbContext _context;

        public SessionsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Doctor/Sessions
        public IActionResult Index()
        {
            var doctorEmail = HttpContext.Session.GetString("Email");
            if (string.IsNullOrEmpty(doctorEmail))
            {
                return RedirectToAction("Login", "Home", new { area = "" });
            }

            // Get doctor's courses
            var doctor = _context.Doctors.FirstOrDefault(d => d.Email == doctorEmail);
            if (doctor == null) return NotFound();

            // Get all sessions for doctor's courses
            var sessions = _context.Sessions
                .Include(s => s.Course)
                .Where(s => s.Course.DoctorId == doctor.DoctorId)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new SessionViewModel
                {
                    SessionId = s.SessionId,
                    CourseId = s.CourseId,
                    CourseName = s.Course.CourseName,
                    CourseCode = s.Course.CourseCode,
                    SessionName = s.SessionName,
                    StartTime = s.StartTime,
                    ExpiryTime = s.ExpiryTime,
                    IsActive = s.IsActive,
                    SessionLink = $"{Request.Scheme}://{Request.Host}/Student/Attendance/Join/{s.SessionToken}",
                    AttendanceCount = s.Attendances.Count,
                    Status = GetSessionStatus(s.StartTime, s.ExpiryTime, s.IsActive)
                })
                .ToList();

            return View(sessions);
        }

        // GET: Doctor/Sessions/Create
        public IActionResult Create()
        {
            var doctorEmail = HttpContext.Session.GetString("Email");
            if (string.IsNullOrEmpty(doctorEmail))
            {
                return RedirectToAction("Login", "Home", new { area = "" });
            }

            var doctor = _context.Doctors.FirstOrDefault(d => d.Email == doctorEmail);
            if (doctor == null) return NotFound();

            var model = new CreateSessionViewModel
            {
                Courses = _context.Courses
                    .Where(c => c.DoctorId == doctor.DoctorId)
                    .Select(c => new SelectListItem
                    {
                        Value = c.CourseId.ToString(),
                        Text = $"{c.CourseCode} - {c.CourseName}"
                    })
                    .ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CreateSessionViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var doctorEmail = HttpContext.Session.GetString("Email");
                    var doctor = _context.Doctors.FirstOrDefault(d => d.Email == doctorEmail);
                    
                    if (doctor == null) return NotFound();

                    // Combine today's date with the selected time
                    var today = DateTime.Today;
                    var startDateTime = new DateTime(
                        today.Year, today.Month, today.Day,
                        model.StartTime.Hour, model.StartTime.Minute, 0
                    );

                    var session = new Session
                    {
                        CourseId = model.CourseId,
                        SessionName = model.SessionName,
                        SessionToken = GenerateSessionToken(),
                        StartTime = startDateTime,
                        ExpiryTime = startDateTime.AddMinutes(model.DurationMinutes),
                        IsActive = true,
                        CreatedBy = doctor.DoctorId
                    };

                    _context.Sessions.Add(session);
                    _context.SaveChanges();

                    TempData["Success"] = "Session created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while creating the session: " + ex.Message);
                }
            }

            // Reload courses if validation fails
            var doctorEmail2 = HttpContext.Session.GetString("Email");
            var doctor2 = _context.Doctors.FirstOrDefault(d => d.Email == doctorEmail2);
            model.Courses = _context.Courses
                .Where(c => c.DoctorId == doctor2.DoctorId)
                .Select(c => new SelectListItem
                {
                    Value = c.CourseId.ToString(),
                    Text = $"{c.CourseCode} - {c.CourseName}"
                })
                .ToList();

            return View(model);
        }

        // GET: Doctor/Sessions/Edit/5
        public IActionResult Edit(int id)
        {
            var session = _context.Sessions
                .Include(s => s.Course)
                .FirstOrDefault(s => s.SessionId == id);

            if (session == null) return NotFound();

            var doctorEmail = HttpContext.Session.GetString("Email");
            var doctor = _context.Doctors.FirstOrDefault(d => d.Email == doctorEmail);

            if (doctor == null || session.Course.DoctorId != doctor.DoctorId)
            {
                return Forbid();
            }

            var model = new EditSessionViewModel
            {
                SessionId = session.SessionId,
                CourseId = session.CourseId,
                SessionName = session.SessionName,
                StartTime = session.StartTime,
                ExpiryTime = session.ExpiryTime,
                IsActive = session.IsActive,
                Courses = _context.Courses
                    .Where(c => c.DoctorId == doctor.DoctorId)
                    .Select(c => new SelectListItem
                    {
                        Value = c.CourseId.ToString(),
                        Text = $"{c.CourseCode} - {c.CourseName}"
                    })
                    .ToList()
            };

            return View(model);
        }

        // POST: Doctor/Sessions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(EditSessionViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var session = _context.Sessions.Find(model.SessionId);
                    if (session == null) return NotFound();

                    session.SessionName = model.SessionName;
                    session.StartTime = model.StartTime;
                    session.ExpiryTime = model.ExpiryTime;
                    session.IsActive = model.IsActive;

                    _context.Sessions.Update(session);
                    _context.SaveChanges();

                    TempData["Success"] = "Session updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while updating the session: " + ex.Message);
                }
            }

            // Reload courses if validation fails
            var doctorEmail = HttpContext.Session.GetString("Email");
            var doctor = _context.Doctors.FirstOrDefault(d => d.Email == doctorEmail);
            model.Courses = _context.Courses
                .Where(c => c.DoctorId == doctor.DoctorId)
                .Select(c => new SelectListItem
                {
                    Value = c.CourseId.ToString(),
                    Text = $"{c.CourseCode} - {c.CourseName}"
                })
                .ToList();

            return View(model);
        }

        // POST: Doctor/Sessions/Delete/5
        [HttpPost]
        public IActionResult Delete(int id)
        {
            try
            {
                var session = _context.Sessions
                    .Include(s => s.Course)
                    .FirstOrDefault(s => s.SessionId == id);

                if (session == null)
                {
                    TempData["Error"] = "Session not found.";
                    return RedirectToAction(nameof(Index));
                }

                var doctorEmail = HttpContext.Session.GetString("Email");
                var doctor = _context.Doctors.FirstOrDefault(d => d.Email == doctorEmail);

                if (doctor == null || session.Course.DoctorId != doctor.DoctorId)
                {
                    TempData["Error"] = "You don't have permission to delete this session.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Sessions.Remove(session);
                _context.SaveChanges();

                TempData["Success"] = "Session deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while deleting the session: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Doctor/Sessions/ViewAttendance/5
        public IActionResult ViewAttendance(int id)
        {
            var session = _context.Sessions
                .Include(s => s.Course)
                .Include(s => s.Attendances)
                    .ThenInclude(a => a.Student)
                .FirstOrDefault(s => s.SessionId == id);

            if (session == null) return NotFound();

            var doctorEmail = HttpContext.Session.GetString("Email");
            var doctor = _context.Doctors.FirstOrDefault(d => d.Email == doctorEmail);

            if (doctor == null || session.Course.DoctorId != doctor.DoctorId)
            {
                return Forbid();
            }

            ViewBag.Session = session;
            return View(session.Attendances.ToList());
        }

        // Helper methods
        private string GenerateSessionToken()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace("/", "_")
                .Replace("+", "-")
                .Substring(0, 22);
        }

        private static string GetSessionStatus(DateTime startTime, DateTime expiryTime, bool isActive)
        {
            var now = DateTime.Now;

            if (!isActive) return "Inactive";
            if (now < startTime) return "Scheduled";
            if (now > expiryTime) return "Expired";
            return "Active";
        }
    }
}
