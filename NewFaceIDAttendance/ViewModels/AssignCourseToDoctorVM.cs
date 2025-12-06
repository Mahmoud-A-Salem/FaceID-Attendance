using Microsoft.AspNetCore.Mvc.Rendering;

namespace NewFaceIDAttendance.ViewModels
{
    public class AssignCourseToDoctorVM
    {
        public int DoctorID { get; set; }
        public string DoctorName { get; set; }

        public List<int> SelectedCourses { get; set; }

        public IEnumerable<SelectListItem> Courses { get; set; }
    }
}
