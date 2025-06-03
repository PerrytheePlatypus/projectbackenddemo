using System;

namespace EduSync.DTOs
{
    public class RecentEnrollmentDto
    {
        public Guid Id { get; set; }
        public string StudentName { get; set; }
        public string CourseTitle { get; set; }
        public DateTime EnrollmentDate { get; set; }
    }
}
