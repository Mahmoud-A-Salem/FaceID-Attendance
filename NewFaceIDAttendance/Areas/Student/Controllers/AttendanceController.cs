using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewFaceIDAttendance.Data;
using NewFaceIDAttendance.Models;

namespace NewFaceIDAttendance.Areas.Student.Controllers
{
    [Area("Student")]
    public class AttendanceController : Controller
    {
        private readonly AppDbContext _context;
        private readonly NewFaceIDAttendance.Services.FaceRecognitionService _faceRecognitionService;
        private readonly ILogger<AttendanceController> _logger;

        public AttendanceController(AppDbContext context, NewFaceIDAttendance.Services.FaceRecognitionService faceRecognitionService, ILogger<AttendanceController> logger)
        {
            _context = context;
            _faceRecognitionService = faceRecognitionService;
            _logger = logger;
        }

        // GET: Student/Attendance/Join/{token}
        [HttpGet("Student/Attendance/Join/{token}")]
        public IActionResult Join(string token)
        {
            // Log for debugging
            Console.WriteLine($"[DEBUG] Token received: {token}");
            
            var session = _context.Sessions
                .Include(s => s.Course)
                    .ThenInclude(c => c.Doctor)
                .FirstOrDefault(s => s.SessionToken == token);

            // Log session details
            if (session != null)
            {
                Console.WriteLine($"[DEBUG] Session found: ID={session.SessionId}, Token={session.SessionToken}");
                Console.WriteLine($"[DEBUG] IsActive={session.IsActive}, StartTime={session.StartTime}, ExpiryTime={session.ExpiryTime}");
                Console.WriteLine($"[DEBUG] Current Time={DateTime.Now}");
            }
            else
            {
                Console.WriteLine($"[DEBUG] NO SESSION FOUND with token: {token}");
            }

            // Validation: Session exists
            if (session == null)
            {
                ViewBag.ErrorTitle = "Invalid Link";
                ViewBag.ErrorMessage = $"This session link is invalid or has been deleted. Token: {token}";
                return View("SessionError");
            }

            // Validation: Session is active
            if (!session.IsActive)
            {
                ViewBag.ErrorTitle = "Session Inactive";
                ViewBag.ErrorMessage = "This session has been deactivated by the instructor.";
                return View("SessionError");
            }

            var now = DateTime.Now;

            // Validation: Session has started
            if (now < session.StartTime)
            {
                ViewBag.ErrorTitle = "Session Not Started";
                ViewBag.ErrorMessage = $"This session will start at {session.StartTime:MMM dd, yyyy hh:mm tt}.";
                ViewBag.StartTime = session.StartTime;
                return View("SessionError");
            }

            // Validation: Session not expired
            if (now > session.ExpiryTime)
            {
                ViewBag.ErrorTitle = "Session Expired";
                ViewBag.ErrorMessage = $"This session expired at {session.ExpiryTime:MMM dd, yyyy hh:mm tt}.";
                return View("SessionError");
            }

            // Get student from session
            var studentEmail = HttpContext.Session.GetString("Email");
            if (string.IsNullOrEmpty(studentEmail))
            {
                // Redirect to login with return URL
                return RedirectToAction("Login", "Home", new 
                { 
                    area = "", 
                    returnUrl = $"/Student/Attendance/Join/{token}" 
                });
            }

            var student = _context.Students
                .Include(s => s.StudentCourses)
                .FirstOrDefault(s => s.Email == studentEmail);

            if (student == null)
            {
                ViewBag.ErrorTitle = "Student Not Found";
                ViewBag.ErrorMessage = "Your student account could not be found.";
                return View("SessionError");
            }

            // Validation: Student is enrolled in course
            var isEnrolled = student.StudentCourses.Any(sc => sc.CourseId == session.CourseId);
            if (!isEnrolled)
            {
                ViewBag.ErrorTitle = "Not Enrolled";
                ViewBag.ErrorMessage = "You are not enrolled in this course.";
                ViewBag.CourseName = session.Course.CourseName;
                return View("SessionError");
            }

            // Check if already attended
            var existingAttendance = _context.Attendances
                .FirstOrDefault(a => a.SessionId == session.SessionId && 
                                    a.StudentId == student.StudentId);

            if (existingAttendance != null)
            {
                ViewBag.SuccessTitle = "Already Marked";
                ViewBag.SuccessMessage = "You have already marked your attendance for this session.";
                ViewBag.AttendanceTime = existingAttendance.CreatedAt;
                return View("AttendanceSuccess");
            }

            // All validations passed - show attendance marking page
            ViewBag.Session = session;
            ViewBag.Student = student;
            return View();
        }

        // POST: Student/Attendance/MarkAttendance
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MarkAttendance(int sessionId, string capturedImage)
        {
            try
            {
                var studentEmail = HttpContext.Session.GetString("Email");
                var student = _context.Students.FirstOrDefault(s => s.Email == studentEmail);

                if (student == null) return NotFound();

                var session = _context.Sessions
                    .Include(s => s.Course)
                    .FirstOrDefault(s => s.SessionId == sessionId);

                if (session == null) return NotFound();

                // Re-validate session (in case time passed since Join was called)
                var now = DateTime.Now;
                if (!session.IsActive || now < session.StartTime || now > session.ExpiryTime)
                {
                    TempData["Error"] = "Session is no longer valid.";
                    return RedirectToAction("Index", "Home");
                }

                // Check for duplicate
                if (_context.Attendances.Any(a => a.SessionId == sessionId && a.StudentId == student.StudentId))
                {
                    TempData["Warning"] = "You have already marked attendance for this session.";
                    return RedirectToAction("Index", "Home");
                }

                // Check Base64 Image
                if (string.IsNullOrEmpty(capturedImage))
                {
                    TempData["Error"] = "Please allow camera access and capture your photo.";
                    // We need to redirect back to Join, but Join requires a token.
                    // Ideally we should post back to the same view or redirect using the session token.
                    // Since we don't have token easily here unless we query it or pass it, we can look it up.
                    return RedirectToAction("Join", new { token = session.SessionToken });
                }

                // Verify Face
                if (student.FaceImage == null || student.FaceImage.Length == 0)
                {
                    TempData["Error"] = "You do not have a face photo registered. Please contact admin.";
                     return RedirectToAction("Join", new { token = session.SessionToken });
                }

                byte[] capturedBytes;
                try
                {
                    // Remove "data:image/jpeg;base64," header
                    var cleanBase64 = capturedImage.Split(',')[1];
                    capturedBytes = Convert.FromBase64String(cleanBase64);
                }
                catch
                {
                    TempData["Error"] = "Invalid image data.";
                     return RedirectToAction("Join", new { token = session.SessionToken });
                }

                bool isMatch = _faceRecognitionService.CompareFaces(student.FaceImage, capturedBytes);

                if (!isMatch)
                {
                    TempData["Error"] = "Face verification failed. Please try again with better lighting and a clear view of your face.";
                     return RedirectToAction("Join", new { token = session.SessionToken });
                }

                // Create attendance record
                var attendance = new Attendance
                {
                    StudentId = student.StudentId,
                    CourseId = session.CourseId,
                    SessionId = sessionId,
                    AttendanceDate = DateOnly.FromDateTime(DateTime.Now),
                    Status = "Present",
                    FaceRecognized = true
                };

                _context.Attendances.Add(attendance);
                _context.SaveChanges();

                ViewBag.SuccessTitle = "Attendance Marked!";
                ViewBag.SuccessMessage = $"Your attendance has been successfully recorded for {session.Course.CourseName}.";
                ViewBag.AttendanceTime = attendance.CreatedAt;
                ViewBag.SessionName = session.SessionName;

                return View("AttendanceSuccess");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking attendance");
                TempData["Error"] = "An error occurred while marking attendance. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }
    }
}
