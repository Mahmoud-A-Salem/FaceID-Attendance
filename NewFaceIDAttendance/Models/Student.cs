namespace NewFaceIDAttendance.Models;

public partial class Student
{
    public int StudentId { get; set; }

    public string FullName { get; set; } = null!;

    public string UniversityId { get; set; } = null!;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public byte[]? FaceImage { get; set; }

    public string? Department { get; set; }

    public int? YearLevel { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? ImagePath { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual ICollection<StudentCourse> StudentCourses { get; set; } = new List<StudentCourse>();
}
