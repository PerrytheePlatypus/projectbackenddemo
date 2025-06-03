namespace EduSync.DTOs
{
    public class TopCourseDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string ThumbnailUrl { get; set; }
        public int EnrollmentsCount { get; set; }
        // AverageRating property removed as requested
        public int Progress { get; set; }
    }
}
