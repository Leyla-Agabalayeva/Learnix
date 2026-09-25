using FluentValidation.TestHelper;
using LMSFinal.Application.Validators.Auth;
using LMSFinal.Contracts.DTOs.Auth;
using LMSFinal.Domain.Enums;

namespace LMSFinal.Tests.Validators
{
    public class RegisterRequestValidatorTests
    {
        private readonly RegisterRequestValidator _validator = new();

        private static RegisterRequest Student() => new()
        {
            Email = "student@lms.com",
            Password = "Password123!",
            FirstName = "Aysel",
            LastName = "Rəhimova",
            Role = UserRole.Student
        };

        private static RegisterRequest Instructor() => Student() with
        {
            Role = UserRole.Instructor,
            ProfessionalTitle = "Senior .NET Developer",
            Specialization = "Backend development",
            YearsOfExperience = 7,
            Bio = "I have built production APIs for seven years and enjoy teaching."
        };

        [Fact]
        public void Student_WithoutProfessionalProfile_IsValid()
        {
            _validator.TestValidate(Student()).ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Instructor_WithFullProfile_IsValid()
        {
            _validator.TestValidate(Instructor()).ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Instructor_WithoutProfessionalTitle_Fails()
        {
            var result = _validator.TestValidate(Instructor() with { ProfessionalTitle = null });

            result.ShouldHaveValidationErrorFor(x => x.ProfessionalTitle);
        }

        [Fact]
        public void Instructor_WithoutExperience_Fails()
        {
            var result = _validator.TestValidate(Instructor() with { YearsOfExperience = null });

            result.ShouldHaveValidationErrorFor(x => x.YearsOfExperience);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(61)]
        public void Instructor_WithExperienceOutOfRange_Fails(int years)
        {
            var result = _validator.TestValidate(Instructor() with { YearsOfExperience = years });

            result.ShouldHaveValidationErrorFor(x => x.YearsOfExperience);
        }

        [Fact]
        public void Instructor_WithTooShortBio_Fails()
        {
            var result = _validator.TestValidate(Instructor() with { Bio = "Too short" });

            result.ShouldHaveValidationErrorFor(x => x.Bio);
        }

        [Fact]
        public void Instructor_WithUnknownEducationLevel_Fails()
        {
            var result = _validator.TestValidate(Instructor() with { EducationLevel = "Wizard" });

            result.ShouldHaveValidationErrorFor(x => x.EducationLevel);
        }

        [Theory]
        [InlineData("not a url")]
        [InlineData("ftp://example.com")]
        public void Instructor_WithInvalidProfileUrl_Fails(string url)
        {
            var result = _validator.TestValidate(Instructor() with { ProfileUrl = url });

            result.ShouldHaveValidationErrorFor(x => x.ProfileUrl);
        }

        [Fact]
        public void Instructor_WithValidOptionalFields_IsValid()
        {
            var request = Instructor() with
            {
                EducationLevel = "Master",
                Organization = "Code Academy",
                ProfileUrl = "https://www.linkedin.com/in/example"
            };

            _validator.TestValidate(request).ShouldNotHaveAnyValidationErrors();
        }
    }
}
