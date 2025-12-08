namespace NewFaceIDAttendance.Models;

public partial class Session
{
    public int SessionId { get; set; }

    public int CourseId { get; set; }

    public string SessionName { get; set; } = null!;

    public string SessionToken { get; set; } = null!;

    public DateTime StartTime { get; set; }

    public DateTime ExpiryTime { get; set; }

    public bool IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    // Navigation properties
    public virtual Course Course { get; set; } = null!;

    public virtual Doctor? Creator { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
}
