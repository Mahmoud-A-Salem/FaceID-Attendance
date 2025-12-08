namespace NewFaceIDAttendance.Models;

public partial class Attendance
{
    public int AttendanceId { get; set; }

    public int? StudentId { get; set; }

    public int? CourseId { get; set; }

    public DateOnly AttendanceDate { get; set; }

    public string? Status { get; set; }

    public bool? FaceRecognized { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? SessionId { get; set; }

    public virtual Course? Course { get; set; }

    public virtual Student? Student { get; set; }

    public virtual Session? Session { get; set; }
}
