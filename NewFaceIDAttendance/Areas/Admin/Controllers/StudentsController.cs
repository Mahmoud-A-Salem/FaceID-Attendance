using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NewFaceIDAttendance.Data;
using NewFaceIDAttendance.Models;
using NewFaceIDAttendance.ViewModels;
using StudentModel = NewFaceIDAttendance.Models.Student;


namespace NewFaceIDAttendance.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class StudentsController : Controller
    {
        private readonly AppDbContext _context;
        public StudentsController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var students = _context.Students.ToList();
            return View(students);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(StudentViewModel model)
        {
            if (string.IsNullOrEmpty(model.Password))
            {
                ModelState.AddModelError("Password", "Password is required.");
            }

            if (ModelState.IsValid)
            {
                // Check for duplicate UniversityID
                if (_context.Students.Any(s => s.UniversityId == model.UniversityID))
                {
                    ModelState.AddModelError("UniversityID", "University ID already exists.");
                    return View(model);
                }

                // Check for duplicate Email in Users
                if (_context.Users.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email is already registered.");
                    return View(model);
                }

                try
                {
                    var student = new StudentModel
                    {
                        FullName = model.FullName,
                        UniversityId = model.UniversityID,
                        Email = model.Email,
                        Phone = model.Phone,
                        Department = model.Department,
                        YearLevel = model.YearLevel
                    };

                    if (model.FaceImageFile != null)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            model.FaceImageFile.CopyTo(memoryStream);
                            student.FaceImage = memoryStream.ToArray();
                        }
                    }

                    _context.Students.Add(student);

                    // Create User record
                    var hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);
                    var user = new User
                    {
                        Email = model.Email,
                        PasswordHash = hashedPassword,
                        Role = "Student"
                    };
                    _context.Users.Add(user);

                    _context.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while saving: " + ex.Message);
                }
            }
            return View(model);
        }

        public IActionResult Edit(int id)
        {
            var student = _context.Students.Find(id);
            if (student == null) return NotFound();

            var model = new StudentViewModel
            {
                StudentID = student.StudentId,
                FullName = student.FullName,
                UniversityID = student.UniversityId,
                Email = student.Email,
                Phone = student.Phone,
                Department = student.Department,
                YearLevel = student.YearLevel,
                FaceImage = student.FaceImage // Restore existing image
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult Edit(StudentViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check for duplicate UniversityID (excluding current student)
                    if (_context.Students.Any(s => s.UniversityId == model.UniversityID && s.StudentId != model.StudentID))
                    {
                        ModelState.AddModelError("UniversityID", "University ID already exists.");
                        return View(model);
                    }

                    var student = _context.Students.Find(model.StudentID);
                    if (student == null) return NotFound();

                    // Find corresponding User to update email/password
                    var user = _context.Users.FirstOrDefault(u => u.Email == student.Email);

                    // Update Student fields
                    student.FullName = model.FullName;
                    student.UniversityId = model.UniversityID;
                    student.Email = model.Email;
                    student.Phone = model.Phone;
                    student.Department = model.Department;
                    student.YearLevel = model.YearLevel;

                    if (model.FaceImageFile != null)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            model.FaceImageFile.CopyTo(memoryStream);
                            student.FaceImage = memoryStream.ToArray();
                        }
                    }

                    // Update User fields
                    if (user != null)
                    {
                        user.Email = model.Email; // Sync email
                        
                        if (!string.IsNullOrEmpty(model.Password))
                        {
                            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
                        }
                        
                        _context.Users.Update(user);
                    }
                    // Optional: If user missing but should exist? Create one? 
                    // For now, assume consistency. But if email changed and user undefined, we might have an issue.
                    // Ideally we track User by ID but here we are loose coupling.
                    // If user is null (maybe created before this fix), we could create one.
                    else if (!string.IsNullOrEmpty(model.Email) && !string.IsNullOrEmpty(model.Password)) 
                    {
                        // Lazily create user if missing
                         var newUser = new User
                        {
                            Email = model.Email,
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                            Role = "Student"
                        };
                         _context.Users.Add(newUser);
                    }


                    _context.Students.Update(student);
                    _context.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while updating: " + ex.Message);
                }
            }
            return View(model);
        }

        public IActionResult Delete(int id)
        {
            var student = _context.Students.Find(id);
            if (student != null)
            {
                _context.Students.Remove(student);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        // GET: AssignCourses
        public IActionResult AssignCourses(int id)
        {
            var student = _context.Students.Find(id);
            if (student == null) return NotFound();

            // Get currently enrolled course IDs for this student
            var enrolledCourseIds = _context.StudentCourses
                .Where(sc => sc.StudentId == id)
                .Select(sc => sc.CourseId)
                .ToList();

            // Get currently enrolled courses with doctor details
            var enrolledCourses = _context.Courses
                .Where(c => enrolledCourseIds.Contains(c.CourseId))
                .Select(c => new
                {
                    c.CourseId,
                    c.CourseCode,
                    c.CourseName,
                    c.YearLevel,
                    DoctorName = c.Doctor != null ? c.Doctor.FullName : "Not Assigned"
                })
                .ToList();

            // Get available courses (not yet enrolled)
            var availableCourses = _context.Courses
                .Where(c => !enrolledCourseIds.Contains(c.CourseId))
                .Select(c => new
                {
                    c.CourseId,
                    c.CourseCode,
                    c.CourseName,
                    c.YearLevel,
                    DoctorName = c.Doctor != null ? c.Doctor.FullName : "Not Assigned"
                })
                .ToList();

            var model = new AssignCourseToStudentVM
            {
                StudentID = student.StudentId,
                StudentName = student.FullName
            };

            ViewBag.EnrolledCourses = enrolledCourses;
            ViewBag.AvailableCourses = availableCourses;

            return View(model);
        }

        // POST: AssignCourses
        [HttpPost]
        public IActionResult AssignCourses(AssignCourseToStudentVM model)
        {
            if (model.SelectedCourses == null || !model.SelectedCourses.Any())
            {
                TempData["Error"] = "Please select at least one course to enroll.";
                return RedirectToAction("AssignCourses", new { id = model.StudentID });
            }

            try
            {
                // Validate student exists
                var student = _context.Students.Find(model.StudentID);
                if (student == null)
                {
                    TempData["Error"] = "Student not found.";
                    return RedirectToAction("Index");
                }

                // Get already enrolled course IDs to prevent duplicates
                var enrolledCourseIds = _context.StudentCourses
                    .Where(sc => sc.StudentId == model.StudentID)
                    .Select(sc => sc.CourseId)
                    .ToList();

                // Filter out already enrolled courses
                var coursesToEnroll = model.SelectedCourses
                    .Where(courseId => courseId.HasValue && !enrolledCourseIds.Contains(courseId.Value))
                    .Select(courseId => courseId.Value)
                    .Distinct()
                    .ToList();

                if (!coursesToEnroll.Any())
                {
                    TempData["Warning"] = "Selected courses are already enrolled.";
                    return RedirectToAction("Index");
                }

                // Create StudentCourse records
                foreach (var courseId in coursesToEnroll)
                {
                    var studentCourse = new StudentCourse
                    {
                        StudentId = model.StudentID,
                        CourseId = courseId
                    };
                    _context.StudentCourses.Add(studentCourse);
                }

                _context.SaveChanges();
                TempData["Success"] = $"Successfully enrolled {student.FullName} in {coursesToEnroll.Count} course(s).";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while enrolling courses: " + ex.Message;
            }

            return RedirectToAction("Index");
        }
    }
}