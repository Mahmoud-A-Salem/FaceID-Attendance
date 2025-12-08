using NewFaceIDAttendance.Models;

namespace NewFaceIDAttendance.Areas.Doctor.ViewModels
{
    public class DoctorDashboardViewModel
    {
        public DoctorDashboardViewModel()
        {
            AssignedCourses = new List<Course>();
        }

        public string DoctorName { get; set; }
        public int TotalStudents { get; set; }
        public int TotalCourses { get; set; }
        public List<Course> AssignedCourses { get; set; }
    }
}
