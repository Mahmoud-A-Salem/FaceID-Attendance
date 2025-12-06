using System.ComponentModel.DataAnnotations;

namespace NewFaceIDAttendance.ViewModels
{
    public class StudentViewModel
    {
        public int StudentID { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required]
        [StringLength(20)]
        public string UniversityID { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        [StringLength(15)]
        public string Phone { get; set; }

        public byte[] FaceImage { get; set; }  // رفع صورة كـ byte array

        [StringLength(50)]
        public string Department { get; set; }

        public int? YearLevel { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // لو هنعمل File Upload
        public IFormFile FaceImageFile { get; set; }
    }
}
