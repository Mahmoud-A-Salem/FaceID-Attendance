using Microsoft.AspNetCore.Mvc.Rendering;

namespace NewFaceIDAttendance.ViewModels
{
    public class AssignCourseToStudentVM
    {
        public int StudentID { get; set; }
        public string StudentName { get; set; }

        public List<int?> SelectedCourses { get; set; } = new();

        public IEnumerable<SelectListItem> Courses { get; set; }
    }
}
