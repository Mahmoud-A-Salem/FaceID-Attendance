using Microsoft.AspNetCore.Mvc;
using NewFaceIDAttendance.Data;
using NewFaceIDAttendance.Models;
public class HomeController : Controller
{
    private readonly AppDbContext _context;

    public HomeController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index([Bind("Email,Password")] LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        string password = "Admin123";

        // عمل hash
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        Console.WriteLine(passwordHash);

        var user = _context.Users.FirstOrDefault(x => x.Email == model.Email);
        if (user == null)
        {
            ModelState.AddModelError("", "Email not found.");
            return View(model);
        }

        // التحقق من كلمة المرور
        if (!BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
        {
            ModelState.AddModelError("", "Invalid password.");
            return View(model);
        }

        // حفظ بيانات المستخدم في Session
        HttpContext.Session.SetString("UserRole", user.Role);
        HttpContext.Session.SetString("Email", user.Email);
        HttpContext.Session.SetInt32("UserID", user.UserID);

        // التوجيه حسب Role
        switch (user.Role)
        {
            case "Admin":
                return RedirectToAction("Index", "Home", new { area = "Admin" });
            case "Doctor":
                // الحصول على DoctorID إذا موجود
                var doctor = _context.Doctors.FirstOrDefault(d => d.Email == user.Email);
                if (doctor != null)
                    HttpContext.Session.SetInt32("DoctorID", doctor.DoctorId);
                return RedirectToAction("Index", "Home", new { area = "Doctor" });
            case "Student":
                // الحصول على StudentID إذا موجود
                var student = _context.Students.FirstOrDefault(s => s.Email == user.Email);
                if (student != null)
                    HttpContext.Session.SetInt32("StudentID", student.StudentId);
                return RedirectToAction("Index", "Home", new { area = "Student" });
            default:
                ModelState.AddModelError("", "Invalid role.");
                return View(model);
        }
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index");
    }
}
