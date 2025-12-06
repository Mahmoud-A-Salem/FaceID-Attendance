using System;
using System.Collections.Generic;

namespace NewFaceIDAttendance.Models;

public partial class Log
{
    public int LogId { get; set; }

    public string? UserType { get; set; }

    public int? UserId { get; set; }

    public string? Action { get; set; }

    public DateTime? Timestamp { get; set; }
}
