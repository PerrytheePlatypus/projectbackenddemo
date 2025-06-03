using AutoMapper;
using EduSync.DTOs;
using EduSync.Models;
using System.Linq;

namespace EduSync.Configurations
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // User mappings
            CreateMap<User, UserDto>();
            CreateMap<UserRegistrationDto, User>();

            // Course mappings
            CreateMap<Course, CourseDto>()
                .ForMember(dest => dest.InstructorName, opt => opt.MapFrom(src => src.Instructor.Name))
                .ForMember(dest => dest.AssessmentCount, opt => opt.MapFrom(src => src.Assessments.Count))
                .ForMember(dest => dest.EnrolledStudentCount, opt => opt.MapFrom(src => src.Enrollments.Count));

            CreateMap<Course, CourseDetailDto>()
                .ForMember(dest => dest.InstructorName, opt => opt.MapFrom(src => src.Instructor.Name))
                .ForMember(dest => dest.EnrolledStudentCount, opt => opt.MapFrom(src => src.Enrollments.Count));

            CreateMap<CourseCreateDto, Course>();
            CreateMap<CourseUpdateDto, Course>();

            // Assessment mappings
            CreateMap<Assessment, AssessmentDto>()
                .ForMember(dest => dest.QuestionCount, opt => opt.MapFrom(src => src.Questions.Count))
                .ForMember(dest => dest.CourseName, opt => opt.MapFrom(src => src.Course.Title));

            CreateMap<Assessment, AssessmentDetailDto>()
                .ForMember(dest => dest.CourseName, opt => opt.MapFrom(src => src.Course.Title));

            CreateMap<Assessment, StudentAssessmentDto>()
                .ForMember(dest => dest.CourseName, opt => opt.MapFrom(src => src.Course.Title));

            CreateMap<AssessmentCreateDto, Assessment>();
            CreateMap<AssessmentUpdateDto, Assessment>();

            // Question mappings
            CreateMap<Question, QuestionDto>()
                .ForMember(dest => dest.Options, opt => opt.MapFrom(src => src.GetOptionsAsList()))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => (int)src.Type));

            CreateMap<Question, StudentQuestionDto>()
                .ForMember(dest => dest.Options, opt => opt.MapFrom(src => src.GetOptionsAsList()))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => (int)src.Type));

            CreateMap<QuestionCreateDto, Question>()
                .ForMember(dest => dest.Options, opt => opt.MapFrom(src => 
                    System.Text.Json.JsonSerializer.Serialize(src.Options, new System.Text.Json.JsonSerializerOptions())))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => (QuestionType)src.Type));

            CreateMap<QuestionUpdateDto, Question>()
                .ForMember(dest => dest.Options, opt => opt.MapFrom(src => 
                    System.Text.Json.JsonSerializer.Serialize(src.Options, new System.Text.Json.JsonSerializerOptions())))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => (QuestionType)src.Type));

            // Result mappings
            CreateMap<Result, ResultDto>()
                .ForMember(dest => dest.AssessmentTitle, opt => opt.MapFrom(src => src.Assessment.Title))
                .ForMember(dest => dest.CourseId, opt => opt.MapFrom(src => src.Assessment.CourseId))
                .ForMember(dest => dest.CourseTitle, opt => opt.MapFrom(src => src.Assessment.Course.Title))
                .ForMember(dest => dest.MaxScore, opt => opt.MapFrom(src => src.Assessment.MaxScore))
                .ForMember(dest => dest.ScorePercentage, opt => opt.MapFrom(src => 
                    src.Assessment.MaxScore > 0 
                        ? (double)src.Score / src.Assessment.MaxScore * 100 
                        : 0));

            // Fix: Add missing map for detailed result view
            CreateMap<Result, ResultDetailDto>();
        }
    }
}
