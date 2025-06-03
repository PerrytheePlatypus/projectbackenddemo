using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EduSync.DTOs
{
    // Request DTOs
    public class CourseCreateDto
    {
        /// <summary>
        /// The title of the course (required, max 100 characters)
        /// </summary>
        [Required(ErrorMessage = "Title is required")]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
        public string Title { get; set; }

        /// <summary>
        /// The description of the course (required, max 2000 characters)
        /// </summary>
        [Required(ErrorMessage = "Description is required")]
        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string Description { get; set; }

        /// <summary>
        /// The category of the course (optional, max 50 characters)
        /// </summary>
        [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters")]
        public string Category { get; set; } = "General";

        /// <summary>
        /// The difficulty level of the course (default: Beginner)
        /// </summary>
        [StringLength(20, ErrorMessage = "Level cannot exceed 20 characters")]
        public string Level { get; set; } = "Beginner";

        /// <summary>
        /// Prerequisites for the course (optional, max 500 characters)
        /// </summary>
        [StringLength(500, ErrorMessage = "Prerequisites cannot exceed 500 characters")]
        public string Prerequisites { get; set; } = "None";
        
        /// <summary>
        /// Media file for the course (optional, max 50MB)
        /// Allowed types: image/jpeg, image/png, image/gif, video/mp4, application/pdf
        /// </summary>
        [DataType(DataType.Upload)]
        [Display(Name = "Course Media")]
        public IFormFile MediaFile { get; set; }
        
        /// <summary>
        /// Instructor ID (optional, will be overridden by authenticated user ID)
        /// </summary>
        public Guid? InstructorId { get; set; }
    }

    public class CourseUpdateDto
    {
        [Required(ErrorMessage = "Title is required")]
        [MaxLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; }

        public IFormFile MediaFile { get; set; }
        public bool RemoveMedia { get; set; }
    }

    public class EnrollmentDto
    {
        [Required(ErrorMessage = "Course ID is required")]
        public Guid CourseId { get; set; }
    }

    // Response DTOs
    public class CourseDto
    {
        public Guid CourseId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public Guid InstructorId { get; set; }
        public string InstructorName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int AssessmentCount { get; set; }
        public int EnrolledStudentCount { get; set; }
        public string MediaFileName { get; set; }
        public string MediaContentType { get; set; }
        public long? MediaFileSize { get; set; }
        public string MediaFileUrl { get; set; }
        // Additional properties needed by the frontend
        public string ImageUrl { get; set; }
        // AverageRating property removed as requested
        public DateTime? EnrollmentDate { get; set; }
    }

    public class CourseDetailDto
    {
        public Guid CourseId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public Guid InstructorId { get; set; }
        public string InstructorName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string MediaFileName { get; set; }
        public string MediaContentType { get; set; }
        public long? MediaFileSize { get; set; }
        public string MediaFileUrl { get; set; }
        public List<AssessmentDto> Assessments { get; set; } = new List<AssessmentDto>();
        public int EnrolledStudentCount { get; set; }
        public bool IsEnrolled { get; set; }
        // Additional properties needed by the frontend
        public string ImageUrl { get; set; }
        // AverageRating property removed as requested
        // RatingCount property removed as requested
        public List<UserDto> EnrolledStudents { get; set; } = new List<UserDto>();
    }
}
