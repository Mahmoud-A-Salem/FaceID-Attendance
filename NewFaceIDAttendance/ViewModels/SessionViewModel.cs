using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace NewFaceIDAttendance.ViewModels
{
    public class CreateSessionViewModel
    {
        [Required(ErrorMessage = "Please select a course")]
        public int CourseId { get; set; }

        [Required(ErrorMessage = "Session name is required")]
        [StringLength(100, ErrorMessage = "Session name cannot exceed 100 characters")]
        public string SessionName { get; set; } = null!;

        [Required(ErrorMessage = "Start time is required")]
        [Display(Name = "Start Time")]
        [DataType(DataType.Time)]
        [DisplayFormat(DataFormatString = "{0:HH:mm}", ApplyFormatInEditMode = true)]
        public DateTime StartTime { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Duration is required")]
        [Range(5, 1440, ErrorMessage = "Duration must be between 5 minutes and 24 hours")]
        [Display(Name = "Duration (Minutes)")]
        public int DurationMinutes { get; set; } = 60;

        public IEnumerable<SelectListItem>? Courses { get; set; }
    }

    public class EditSessionViewModel
    {
        public int SessionId { get; set; }

        [Required]
        public int CourseId { get; set; }

        [Required]
        [StringLength(100)]
        public string SessionName { get; set; } = null!;

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime ExpiryTime { get; set; }

        public bool IsActive { get; set; }

        public IEnumerable<SelectListItem>? Courses { get; set; }
    }

    public class SessionViewModel
    {
        public int SessionId { get; set; }
        public int CourseId { get; set; }
        public string CourseName { get; set; } = null!;
        public string CourseCode { get; set; } = null!;
        public string SessionName { get; set; } = null!;
        public DateTime StartTime { get; set; }
        public DateTime ExpiryTime { get; set; }
        public bool IsActive { get; set; }
        public string SessionLink { get; set; } = null!;
        public int AttendanceCount { get; set; }
        public string Status { get; set; } = null!;
        public string? DoctorName { get; set; }
    }
}
