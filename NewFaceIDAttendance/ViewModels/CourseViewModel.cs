using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace NewFaceIDAttendance.ViewModels
{
    public class CourseViewModel
    {
        public int CourseID { get; set; }

        [Required]
        [StringLength(100)]
        public string CourseName { get; set; }

        [Required]
        [StringLength(20)]
        public string CourseCode { get; set; }

        public int? DoctorID { get; set; } // Optional

        public int? YearLevel { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Dropdown
        public IEnumerable<SelectListItem> Doctors { get; set; }
    }
}
