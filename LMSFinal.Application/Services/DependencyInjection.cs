using AutoMapper;
using FluentValidation;
using LMSFinal.Application.Interfaces;
using LMSFinal.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Application.Services
{

    public static class ApplicationServiceRegistration
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();

            services.AddAutoMapper(cfg => { }, assembly);

            services.AddValidatorsFromAssembly(assembly);

            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IHeroSlideService, HeroSlideService>();
            services.AddScoped<ICourseService, CourseService>();
            services.AddScoped<IModuleService, ModuleService>();
            services.AddScoped<ILessonService, LessonService>();
            services.AddScoped<IEnrollmentService, EnrollmentService>();
            services.AddScoped<IQuizService, QuizService>();
            services.AddScoped<IProgressService, ProgressService>();
            services.AddScoped<IGradeBookService, GradeBookService>();
            services.AddScoped<IReviewService, ReviewService>();
            services.AddScoped<ICertificateService, CertificateService>();
            services.AddScoped<IWishlistService, WishlistService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IAnalyticsService, AnalyticsService>();

            return services;
        }
    }


}
