using Microsoft.AspNetCore.Mvc;
using NewFaceIDAttendance.Data;
using NewFaceIDAttendance.Models;
using NewFaceIDAttendance.ViewModels;


namespace NewFaceIDAttendance.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class DoctorsController : Controller
    {
        private readonly AppDbContext _context;

        public DoctorsController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var doctors = _context.Doctors.ToList();
            return View(doctors);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(DoctorViewModel model)
        {
            if (string.IsNullOrEmpty(model.Password))
            {
                ModelState.AddModelError("Password", "Password is required.");
            }

            if (ModelState.IsValid)
            {
                // Check if email already exists in Users
                if (_context.Users.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email is already registered.");
                    return View(model);
                }

                var doctor = new NewFaceIDAttendance.Models.Doctor
                {
                    FullName = model.FullName,
                    Email = model.Email,
                    Department = model.Department,
                    // Note: Doctor model might not have Password field directly if we only store hash in User,
                    // or if it has PasswordHash, we map it. 
                    // Based on previous ViewModels, DoctorViewModel had Password. 
                    // Let's check Doctor model definition if needed.
                    // Assuming Doctor table interacts with User table via Email.
                };

                _context.Doctors.Add(doctor);

                // Create User
                var user = new User
                {
                    Email = model.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    Role = "Doctor"
                };
                _context.Users.Add(user);

                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(model);
        }

        public IActionResult Edit(int id)
        {
            var doctor = _context.Doctors.Find(id);
            if (doctor == null) return NotFound();

            var model = new DoctorViewModel
            {
                DoctorID = doctor.DoctorId,
                FullName = doctor.FullName,
                Email = doctor.Email,
                Department = doctor.Department,
                // Password is purposefully left empty
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult Edit(DoctorViewModel model)
        {
            if (ModelState.IsValid)
            {
                var doctor = _context.Doctors.Find(model.DoctorID);
                if (doctor == null) return NotFound();

                // Find User
                var user = _context.Users.FirstOrDefault(u => u.Email == doctor.Email);

                // Update Doctor
                doctor.FullName = model.FullName;
                doctor.Email = model.Email;
                doctor.Department = model.Department;

                // Update User
                if (user != null)
                {
                    user.Email = model.Email;
                    if (!string.IsNullOrEmpty(model.Password))
                    {
                        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
                    }
                    _context.Users.Update(user);
                }
                 else if (!string.IsNullOrEmpty(model.Email) && !string.IsNullOrEmpty(model.Password))
                {
                     // Lazily create user if missing
                     var newUser = new User
                    {
                        Email = model.Email,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                        Role = "Doctor"
                    };
                     _context.Users.Add(newUser);
                }

                _context.Doctors.Update(doctor);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(model);
        }

        public IActionResult Delete(int id)
        {
            var doctor = _context.Doctors.Find(id);
            if (doctor != null)
            {
                // Also delete User? Or keep?
                // Usually keeping User might be safe or delete it.
                // Let's delete User to keep cleanup.
                var user = _context.Users.FirstOrDefault(u => u.Email == doctor.Email);
                if (user != null)
                {
                    _context.Users.Remove(user);
                }

                _context.Doctors.Remove(doctor);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}
