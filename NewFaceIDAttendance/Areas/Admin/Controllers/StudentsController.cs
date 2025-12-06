using Microsoft.AspNetCore.Mvc;
using NewFaceIDAttendance.Data;
using NewFaceIDAttendance.Models;
using NewFaceIDAttendance.ViewModels;


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
            if (ModelState.IsValid)
            {
                // Check for duplicate UniversityID
                if (_context.Students.Any(s => s.UniversityId == model.UniversityID))
                {
                    ModelState.AddModelError("UniversityID", "University ID already exists. Please use a unique ID.");
                    return View(model);
                }

                try
                {
                    var student = new Student
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
                        ModelState.AddModelError("UniversityID", "University ID already exists. Please use a unique ID.");
                        return View(model);
                    }

                    var student = _context.Students.Find(model.StudentID);
                    if (student == null) return NotFound();

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
    }
}