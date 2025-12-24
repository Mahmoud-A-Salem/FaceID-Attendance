using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewFaceIDAttendance.Data;
using NewFaceIDAttendance.Areas.Doctor.ViewModels;

namespace NewFaceIDAttendance.Areas.Doctor.Controllers
{
    [Area("Doctor")]
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Security Check: Verify User is logged in and is a Doctor
            var role = HttpContext.Session.GetString("UserRole");
            var doctorId = HttpContext.Session.GetInt32("DoctorID");

            if (string.IsNullOrEmpty(role) || role != "Doctor" || doctorId == null)
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            // Fetch Doctor Data
            var doctor = await _context.Doctors
                .Include(d => d.Courses)
                .ThenInclude(c => c.StudentCourses)
                .FirstOrDefaultAsync(d => d.DoctorId == doctorId);

            if (doctor == null)
            {
                return RedirectToAction("Logout", "Home", new { area = "" });
            }

            // Prepare ViewModel
            var model = new DoctorDashboardViewModel
            {
                DoctorName = doctor.FullName,
                AssignedCourses = doctor.Courses.ToList(),
                TotalCourses = doctor.Courses.Count,
                TotalStudents = doctor.Courses.Sum(c => c.StudentCourses.Count) 
                // Note: This sums enrollments. Unique students across courses would require distinct count if needed.
            };

            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            // Security Check
            var doctorId = HttpContext.Session.GetInt32("DoctorID");
            if (doctorId == null) return RedirectToAction("Index", "Home", new { area = "" });

            // Fetch Course and Verify Ownership
            var course = await _context.Courses
                .Include(c => c.StudentCourses)
                    .ThenInclude(sc => sc.Student)
                .Include(c => c.Attendances)
                .FirstOrDefaultAsync(c => c.CourseId == id && c.DoctorId == doctorId);

            if (course == null)
            {
                return NotFound();
            }

            // Calculate Stats
            // Use logical sessions from the database
            var totalSessions = await _context.Sessions.CountAsync(s => s.CourseId == id);
            
            // Calculate Total Attendance (Present)
            var totalPresent = await _context.Attendances
                .Where(a => a.CourseId == id && a.Status == "Present")
                .CountAsync();

            var totalStudents = course.StudentCourses.Count;
            
            double averageAttendance = 0;
            if (totalSessions > 0 && totalStudents > 0)
            {
                averageAttendance = Math.Round((double)totalPresent / (totalStudents * totalSessions) * 100, 1);
            }

            var studentViewModels = new List<StudentAttendanceViewModel>();

            foreach (var sc in course.StudentCourses)
            {
                var studentStats = new StudentAttendanceViewModel
                {
                    StudentId = sc.Student.StudentId,
                    StudentName = sc.Student.FullName,
                    UniversityId = sc.Student.UniversityId,
                };

                // Calculate attendance from the loaded Attendances collection (filtered in memory for this student)
                // Note: Ensure Attendances are loaded. We did Include(c => c.Attendances).
                // Better approach might be filtering at query level if data is large, but for now in-memory is okay for reasonable sizes.
                
                var studentAttendances = course.Attendances
                    .Where(a => a.StudentId == sc.StudentId)
                    .ToList();

                // Assuming "Status" string "Present" / "Absent" usage, or FaceRecognized bool.
                // Let's assume Status 'Present' means present.
                // You might need to adjust based on your actual Status values.
                studentStats.PresentCount = studentAttendances.Count(a => a.Status == "Present");
                studentStats.AbsentCount = studentAttendances.Count(a => a.Status == "Absent");

                // If total sessions is 0, avoid divide by zero
                if (totalSessions > 0)
                {
                    studentStats.AttendancePercentage = Math.Round((double)studentStats.PresentCount / totalSessions * 100, 1);
                }
                else
                {
                    studentStats.AttendancePercentage = 0;
                }

                studentViewModels.Add(studentStats);
            }

            var model = new CourseDetailsViewModel
            {
                CourseId = course.CourseId,
                CourseName = course.CourseName,
                CourseCode = course.CourseCode,
                TotalSessions = totalSessions,
                AverageAttendancePercentage = averageAttendance,
                Students = studentViewModels
            };

            return View(model);
        }

        public async Task<IActionResult> MyCourses()
        {
            var doctorId = HttpContext.Session.GetInt32("DoctorID");
            if (doctorId == null) return RedirectToAction("Index", "Home", new { area = "" });

            var courses = await _context.Courses
                .Include(c => c.StudentCourses)
                .Where(c => c.DoctorId == doctorId)
                .ToListAsync();

            return View(courses);
        }

        public async Task<IActionResult> Students()
        {
            var doctorId = HttpContext.Session.GetInt32("DoctorID");
            if (doctorId == null) return RedirectToAction("Index", "Home", new { area = "" });

            // Fetch distinct students enrolled in ANY of this doctor's courses
            // And maybe calculate some aggregate stats if needed, or just list them.
            // For now, let's just list them.
            
            var doctorCourses = await _context.Courses
                .Where(c => c.DoctorId == doctorId)
                .Select(c => c.CourseId)
                .ToListAsync();

            var students = await _context.StudentCourses
                .Include(SC => SC.Student)
                .Where(sc => sc.CourseId.HasValue && doctorCourses.Contains(sc.CourseId.Value))
                .Select(sc => sc.Student)
                .Distinct()
                .ToListAsync();

            // Optionally, we could wrap this in a ViewModel if we want to show which courses they are in, etc.
            // But for a simple list, passing the Student model list is fine, or a simple VM.
            // Let's stick to the Student model for simplicity unless more is requested.

            return View(students);
        }
    }
}
