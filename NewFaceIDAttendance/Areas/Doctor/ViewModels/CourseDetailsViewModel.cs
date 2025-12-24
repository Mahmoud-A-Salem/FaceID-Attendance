using NewFaceIDAttendance.Models;

namespace NewFaceIDAttendance.Areas.Doctor.ViewModels
{
    public class CourseDetailsViewModel
    {
        public CourseDetailsViewModel()
        {
            Students = new List<StudentAttendanceViewModel>();
        }

        public int CourseId { get; set; }
        public string CourseName { get; set; }
        public string CourseCode { get; set; }
        public int TotalSessions { get; set; }
        public double AverageAttendancePercentage { get; set; }
        public List<StudentAttendanceViewModel> Students { get; set; }
    }

    public class StudentAttendanceViewModel
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string UniversityId { get; set; }
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public double AttendancePercentage { get; set; }
    }
}
