using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewFaceIDAttendance.Data;
using NewFaceIDAttendance.Models;
using NewFaceIDAttendance.ViewModels;

namespace NewFaceIDAttendance.Areas.Student.Controllers
{
    [Area("Student")]
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Student/Home/Index (Dashboard)
        public IActionResult Index()
        {
            var studentEmail = HttpContext.Session.GetString("Email");
            if (string.IsNullOrEmpty(studentEmail))
            {
                return RedirectToAction("Login", "Home", new { area = "" });
            }

            var student = _context.Students
                .Include(s => s.StudentCourses)
                    .ThenInclude(sc => sc.Course)
                        .ThenInclude(c => c.Doctor)
                .FirstOrDefault(s => s.Email == studentEmail);

            if (student == null) return NotFound();

            // Get enrolled courses
            var enrolledCourses = student.StudentCourses
                .Where(sc => sc.Course != null)
                .Select(sc => new CourseInfo
                {
                    CourseId = sc.Course.CourseId,
                    CourseName = sc.Course.CourseName,
                    CourseCode = sc.Course.CourseCode,
                    DoctorName = sc.Course.Doctor?.FullName,
                    TotalSessions = _context.Sessions.Count(s => s.CourseId == sc.Course.CourseId),
                    AttendedSessions = _context.Attendances.Count(a => 
                        a.StudentId == student.StudentId && 
                        a.CourseId == sc.Course.CourseId),
                    AttendancePercentage = CalculateAttendancePercentage(
                        student.StudentId, 
                        sc.Course.CourseId)
                })
                .ToList();

            // Get active sessions for enrolled courses
            var now = DateTime.Now;
            var courseIds = student.StudentCourses.Select(sc => sc.CourseId).ToList();
            
            var activeSessions = _context.Sessions
                .Include(s => s.Course)
                .Where(s => courseIds.Contains(s.CourseId) &&
                           s.IsActive &&
                           s.StartTime <= now &&
                           s.ExpiryTime > now)
                .Select(s => new ActiveSessionInfo
                {
                    SessionId = s.SessionId,
                    CourseName = s.Course.CourseName,
                    CourseCode = s.Course.CourseCode,
                    SessionName = s.SessionName,
                    ExpiryTime = s.ExpiryTime,
                    SessionToken = s.SessionToken,
                    AlreadyAttended = _context.Attendances.Any(a => 
                        a.SessionId == s.SessionId && 
                        a.StudentId == student.StudentId),
                    TimeRemaining = s.ExpiryTime - now
                })
                .ToList();

            var model = new StudentDashboardViewModel
            {
                Student = student,
                EnrolledCourses = enrolledCourses,
                ActiveSessions = activeSessions,
                TotalAttendance = _context.Attendances.Count(a => a.StudentId == student.StudentId)
            };

            return View(model);
        }

        // GET: Student/Home/MyCourses
        public IActionResult MyCourses()
        {
            var studentEmail = HttpContext.Session.GetString("Email");
            if (string.IsNullOrEmpty(studentEmail))
            {
                return RedirectToAction("Login", "Home", new { area = "" });
            }

            var student = _context.Students
                .Include(s => s.StudentCourses)
                    .ThenInclude(sc => sc.Course)
                        .ThenInclude(c => c.Doctor)
                .FirstOrDefault(s => s.Email == studentEmail);

            if (student == null) return NotFound();

            var courses = student.StudentCourses
                .Where(sc => sc.Course != null)
                .Select(sc => new CourseInfo
                {
                    CourseId = sc.Course.CourseId,
                    CourseName = sc.Course.CourseName,
                    CourseCode = sc.Course.CourseCode,
                    DoctorName = sc.Course.Doctor?.FullName,
                    TotalSessions = _context.Sessions.Count(s => s.CourseId == sc.Course.CourseId),
                    AttendedSessions = _context.Attendances.Count(a => 
                        a.StudentId == student.StudentId && 
                        a.CourseId == sc.Course.CourseId),
                    AttendancePercentage = CalculateAttendancePercentage(
                        student.StudentId, 
                        sc.Course.CourseId)
                })
                .ToList();

            ViewBag.StudentName = student.FullName;
            return View(courses);
        }

        // GET: Student/Home/MyAttendance
        public IActionResult MyAttendance()
        {
            var studentEmail = HttpContext.Session.GetString("Email");
            if (string.IsNullOrEmpty(studentEmail))
            {
                return RedirectToAction("Login", "Home", new { area = "" });
            }

            var student = _context.Students.FirstOrDefault(s => s.Email == studentEmail);
            if (student == null) return NotFound();

            var attendances = _context.Attendances
                .Include(a => a.Course)
                .Include(a => a.Session)
                .Where(a => a.StudentId == student.StudentId)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new AttendanceRecord
                {
                    AttendanceId = a.AttendanceId,
                    CourseName = a.Course.CourseName,
                    CourseCode = a.Course.CourseCode,
                    SessionName = a.Session != null ? a.Session.SessionName : null,
                    AttendanceDate = a.AttendanceDate,
                    Status = a.Status ?? "Present",
                    FaceRecognized = a.FaceRecognized ?? false,
                    CreatedAt = a.CreatedAt
                })
                .ToList();

            var stats = new Dictionary<string, int>
            {
                ["Total"] = attendances.Count,
                ["Present"] = attendances.Count(a => a.Status == "Present"),
                ["FaceRecognized"] = attendances.Count(a => a.FaceRecognized)
            };

            var model = new StudentAttendanceViewModel
            {
                Attendances = attendances,
                AttendanceStats = stats
            };

            ViewBag.StudentName = student.FullName;
            return View(model);
        }

        // Helper method
        private int CalculateAttendancePercentage(int studentId, int courseId)
        {
            var totalSessions = _context.Sessions.Count(s => s.CourseId == courseId);
            if (totalSessions == 0) return 0;

            var attendedSessions = _context.Attendances.Count(a => 
                a.StudentId == studentId && 
                a.CourseId == courseId);

            return (int)Math.Round((double)attendedSessions / totalSessions * 100);
        }
    }
}
