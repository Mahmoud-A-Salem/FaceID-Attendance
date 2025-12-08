using System.ComponentModel.DataAnnotations;

namespace NewFaceIDAttendance.ViewModels
{
    public class DoctorViewModel
    {
        public int DoctorID { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        public string Password { get; set; }  // هيتحول لـ Hash قبل التخزين

        [StringLength(50)]
        public string Department { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
