using System.ComponentModel.DataAnnotations;
using NewFaceIDAttendance.Models;

namespace NewFaceIDAttendance.ViewModels
{
    public class StudentDashboardViewModel
    {
        public Student Student { get; set; } = null!;
        public List<CourseInfo> EnrolledCourses { get; set; } = new();
        public List<ActiveSessionInfo> ActiveSessions { get; set; } = new();
        public int TotalAttendance { get; set; }
        public Dictionary<int, int> CourseAttendancePercentage { get; set; } = new();
    }

    public class CourseInfo
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; } = null!;
        public string CourseCode { get; set; } = null!;
        public string? DoctorName { get; set; }
        public int TotalSessions { get; set; }
        public int AttendedSessions { get; set; }
        public int AttendancePercentage { get; set; }
    }

    public class ActiveSessionInfo
    {
        public int SessionId { get; set; }
        public string CourseName { get; set; } = null!;
        public string CourseCode { get; set; } = null!;
        public string SessionName { get; set; } = null!;
        public DateTime ExpiryTime { get; set; }
        public string SessionToken { get; set; } = null!;
        public bool AlreadyAttended { get; set; }
        public TimeSpan TimeRemaining { get; set; }
    }

    public class StudentAttendanceViewModel
    {
        public List<AttendanceRecord> Attendances { get; set; } = new();
        public Dictionary<string, int> AttendanceStats { get; set; } = new();
    }

    public class AttendanceRecord
    {
        public int AttendanceId { get; set; }
        public string CourseName { get; set; } = null!;
        public string CourseCode { get; set; } = null!;
        public string? SessionName { get; set; }
        public DateOnly AttendanceDate { get; set; }
        public string Status { get; set; } = null!;
        public bool FaceRecognized { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
