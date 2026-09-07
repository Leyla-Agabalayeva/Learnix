using AutoMapper;
using LMSFinal.Application.Common;
using LMSFinal.Contracts.DTOs.Categories;
using LMSFinal.Contracts.DTOs.Courses;
using LMSFinal.Contracts.DTOs.Enrollments;
using LMSFinal.Contracts.DTOs.Lessons;
using LMSFinal.Contracts.DTOs.Modules;
using LMSFinal.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LMSFinal.Application.Mappings
{



    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // --- Categories  ---
            CreateMap<Category, CategoryDto>();
            CreateMap<CategoryTranslation, CategoryTranslationDto>()
                .ForCtorParam(nameof(CategoryTranslationDto.LanguageCode),
                    opt => opt.MapFrom(src => src.LanguageCode.ToString()));

            // --- Courses ---
            CreateMap<Course, CourseDto>()
                .ForCtorParam(nameof(CourseDto.InstructorName),
                    opt => opt.MapFrom(src => $"{src.Instructor.FirstName} {src.Instructor.LastName}"))
                .ForCtorParam(nameof(CourseDto.CategorySlug),
                    opt => opt.MapFrom(src => src.Category.Slug))
                .ForCtorParam(nameof(CourseDto.Level),
                    opt => opt.MapFrom(src => src.Level.ToString()))
                .ForCtorParam(nameof(CourseDto.Status),
                    opt => opt.MapFrom(src => src.Status.ToString()));

            CreateMap<Course, CourseSummaryDto>()
                .ForCtorParam(nameof(CourseSummaryDto.Level),
                    opt => opt.MapFrom(src => src.Level.ToString()))
                .ForCtorParam(nameof(CourseSummaryDto.Status),
                    opt => opt.MapFrom(src => src.Status.ToString()));

            CreateMap<CourseTranslation, CourseTranslationDto>()
                .ForCtorParam(nameof(CourseTranslationDto.LanguageCode),
                    opt => opt.MapFrom(src => src.LanguageCode.ToString()))
                .ForCtorParam(nameof(CourseTranslationDto.WhatYouWillLearn),
                    opt => opt.MapFrom(src => JsonListHelper.DeserializeStringList(src.WhatYouWillLearn)));

            // --- Modules ---
            CreateMap<Module, ModuleDto>();
            CreateMap<ModuleTranslation, ModuleTranslationDto>()
                .ForCtorParam(nameof(ModuleTranslationDto.LanguageCode),
                    opt => opt.MapFrom(src => src.LanguageCode.ToString()));

            // --- Lessons  ---
            CreateMap<Lesson, LessonDto>()
                .ForCtorParam(nameof(LessonDto.QuizId),
                    opt => opt.MapFrom(src => src.Quiz != null ? src.Quiz.Id : (Guid?)null));
            CreateMap<LessonTranslation, LessonTranslationDto>()
                .ForCtorParam(nameof(LessonTranslationDto.LanguageCode),
                    opt => opt.MapFrom(src => src.LanguageCode.ToString()));

            // --- Enrollments  ---
            CreateMap<Enrollment, StudentEnrollmentDto>()
                .ForCtorParam(nameof(StudentEnrollmentDto.Status),
                    opt => opt.MapFrom(src => src.Status.ToString()));

            CreateMap<Enrollment, CourseEnrollmentDto>()
                .ForCtorParam(nameof(CourseEnrollmentDto.StudentName),
                    opt => opt.MapFrom(src => $"{src.Student.FirstName} {src.Student.LastName}"))
                .ForCtorParam(nameof(CourseEnrollmentDto.StudentEmail),
                    opt => opt.MapFrom(src => src.Student.Email))
                .ForCtorParam(nameof(CourseEnrollmentDto.Status),
                    opt => opt.MapFrom(src => src.Status.ToString()));
        }
    }

}

