using LMSFinal.Application.Interfaces;
using LMSFinal.Contracts.DTOs.Auth;
using LMSFinal.Domain.Entities;
using LMSFinal.Domain.Enums;
using LMSFinal.Infrastructure.Email;
using LMSFinal.Infrastructure.Identity;
using LMSFinal.Tests.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Moq;
using System.Text;

namespace LMSFinal.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManager = MockUserManager.Create();
        private readonly Mock<IJwtTokenService> _tokenService = new();
        private readonly Mock<IEmailSender> _emailSender = new();

        private AuthService CreateSut()
        {
            _tokenService
                .Setup(service => service.GenerateToken(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>()))
                .Returns(("test-jwt-token", DateTime.UtcNow.AddHours(2)));

            return new AuthService(_userManager.Object, _tokenService.Object, _emailSender.Object, Options.Create(new EmailSettings()));
        }

        private static RegisterRequest RegisterRequest(UserRole role = UserRole.Student) => new()
        {
            Email = "new@lms.com",
            Password = "Password123!",
            FirstName = "New",
            LastName = "User",
            Role = role
        };

        // ------------------------------------------------------------------
        // Регистрация
        // ------------------------------------------------------------------

        [Theory]
        [InlineData(UserRole.Student)]
        [InlineData(UserRole.Instructor)]
        public async Task RegisterAsync_NewEmail_CreatesUserWithRequestedRoleAndReturnsToken(UserRole role)
        {
            _userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);
            _userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
            _userManager.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);

            var result = await CreateSut().RegisterAsync(RegisterRequest(role));

            Assert.True(result.IsSuccess);
            Assert.Equal("test-jwt-token", result.Value!.Token);
            Assert.Equal(role.ToString(), Assert.Single(result.Value.Roles));

            _userManager.Verify(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), role.ToString()), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_Instructor_SavesProfessionalProfile()
        {
            ApplicationUser? created = null;

            _userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);
            _userManager
                .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .Callback<ApplicationUser, string>((user, _) => created = user)
                .ReturnsAsync(IdentityResult.Success);
            _userManager.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);

            var request = RegisterRequest(UserRole.Instructor) with
            {
                ProfessionalTitle = "  Senior .NET Developer  ",
                Specialization = "Backend",
                YearsOfExperience = 7,
                EducationLevel = "Master",
                Organization = "   ",
                Bio = "Seven years of building APIs."
            };

            var result = await CreateSut().RegisterAsync(request);

            Assert.True(result.IsSuccess);
            Assert.NotNull(created);
            Assert.Equal("Senior .NET Developer", created!.ProfessionalTitle);
            Assert.Equal("Backend", created.Specialization);
            Assert.Equal(7, created.YearsOfExperience);
            Assert.Equal("Master", created.EducationLevel);
            Assert.Null(created.Organization);
        }

        [Fact]
        public async Task RegisterAsync_Student_IgnoresProfessionalProfileFields()
        {
            ApplicationUser? created = null;

            _userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);
            _userManager
                .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .Callback<ApplicationUser, string>((user, _) => created = user)
                .ReturnsAsync(IdentityResult.Success);
            _userManager.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);

            var request = RegisterRequest(UserRole.Student) with
            {
                ProfessionalTitle = "Should be ignored",
                YearsOfExperience = 10
            };

            await CreateSut().RegisterAsync(request);

            Assert.NotNull(created);
            Assert.Null(created!.ProfessionalTitle);
            Assert.Null(created.YearsOfExperience);
        }

        [Fact]
        public async Task RegisterAsync_AdminRole_IsRejected()
        {
            var result = await CreateSut().RegisterAsync(RegisterRequest(UserRole.Admin));

            Assert.False(result.IsSuccess);
            _userManager.Verify(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_EmailAlreadyTaken_FailsWithoutCreatingUser()
        {
            _userManager.Setup(m => m.FindByEmailAsync("new@lms.com")).ReturnsAsync(TestData.User());

            var result = await CreateSut().RegisterAsync(RegisterRequest());

            Assert.False(result.IsSuccess);
            _userManager.Verify(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_WeakPasswordRejectedByIdentity_ReturnsFailure()
        {
            _userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);
            _userManager
                .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password too short." }));

            var result = await CreateSut().RegisterAsync(RegisterRequest());

            Assert.False(result.IsSuccess);
            Assert.Contains("Password too short.", result.Error);
        }

        [Fact]
        public async Task RegisterAsync_RoleAssignmentFails_DeletesTheCreatedUser()
        {
            _userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);
            _userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
            _userManager
                .Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Role does not exist." }));
            _userManager.Setup(m => m.DeleteAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

            var result = await CreateSut().RegisterAsync(RegisterRequest());

            Assert.False(result.IsSuccess);
            _userManager.Verify(m => m.DeleteAsync(It.IsAny<ApplicationUser>()), Times.Once);
        }

        // ------------------------------------------------------------------
        // Вход
        // ------------------------------------------------------------------

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsTokenWithUserRoles()
        {
            var user = TestData.User("Leyla", "Agabalayeva");

            _userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
            _userManager.Setup(m => m.CheckPasswordAsync(user, "Correct123!")).ReturnsAsync(true);
            _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Student" });

            var result = await CreateSut().LoginAsync(new LoginRequest { Email = user.Email!, Password = "Correct123!" });

            Assert.True(result.IsSuccess);
            Assert.Equal("test-jwt-token", result.Value!.Token);
            Assert.Equal("Student", Assert.Single(result.Value.Roles));
            Assert.Equal(user.Id, result.Value.UserId);
        }

        [Fact]
        public async Task LoginAsync_WrongPassword_FailsAndIssuesNoToken()
        {
            var user = TestData.User();

            _userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
            _userManager.Setup(m => m.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(false);

            var result = await CreateSut().LoginAsync(new LoginRequest { Email = user.Email!, Password = "Wrong123!" });

            Assert.False(result.IsSuccess);
            _tokenService.Verify(s => s.GenerateToken(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_UnknownEmailAndWrongPassword_ReturnTheSameMessage()
        {
            // Разные сообщения позволили бы перебором выяснить, какие email зарегистрированы.
            var user = TestData.User();

            _userManager.Setup(m => m.FindByEmailAsync("missing@lms.com")).ReturnsAsync((ApplicationUser?)null);
            _userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
            _userManager.Setup(m => m.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(false);

            var sut = CreateSut();
            var unknownEmail = await sut.LoginAsync(new LoginRequest { Email = "missing@lms.com", Password = "Whatever1!" });
            var wrongPassword = await sut.LoginAsync(new LoginRequest { Email = user.Email!, Password = "Whatever1!" });

            Assert.False(unknownEmail.IsSuccess);
            Assert.False(wrongPassword.IsSuccess);
            Assert.Equal(unknownEmail.Error, wrongPassword.Error);
        }

        // ------------------------------------------------------------------
        // Сброс пароля
        // ------------------------------------------------------------------

        [Fact]
        public async Task ForgotPasswordAsync_KnownEmail_SendsEmailWithResetLink()
        {
            var user = TestData.User("Leyla", "Agabalayeva");

            _userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
            _userManager.Setup(m => m.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("raw-token");

            await CreateSut().ForgotPasswordAsync(new ForgotPasswordRequest { Email = user.Email! });

            _emailSender.Verify(sender => sender.SendAsync(
                user.Email!,
                It.IsAny<string>(),
                It.Is<string>(body => body.Contains("reset-password.html") && body.Contains($"email={Uri.EscapeDataString(user.Email!)}")),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ForgotPasswordAsync_UnknownEmail_SendsNoEmailAndDoesNotThrow()
        {
            _userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

            await CreateSut().ForgotPasswordAsync(new ForgotPasswordRequest { Email = "missing@lms.com" });

            _emailSender.Verify(sender => sender.SendAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ResetPasswordAsync_ValidToken_ChangesPassword()
        {
            var user = TestData.User();

            _userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
            _userManager
                .Setup(m => m.ResetPasswordAsync(user, "raw-token", "NewPassword123!"))
                .ReturnsAsync(IdentityResult.Success);
            var encodedToken = Convert.ToBase64String(Encoding.UTF8.GetBytes("raw-token"))
                .Replace('+', '-').Replace('/', '_').TrimEnd('=');

            var result = await CreateSut().ResetPasswordAsync(new ResetPasswordRequest
            {
                Email = user.Email!,
                Token = encodedToken,
                NewPassword = "NewPassword123!"
            });

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task ResetPasswordAsync_IdentityRejectsToken_ReturnsFailure()
        {
            var user = TestData.User();

            _userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
            _userManager
                .Setup(m => m.ResetPasswordAsync(user, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token." }));

            var encodedToken = Convert.ToBase64String(Encoding.UTF8.GetBytes("expired-token"))
                .Replace('+', '-').Replace('/', '_').TrimEnd('=');

            var result = await CreateSut().ResetPasswordAsync(new ResetPasswordRequest
            {
                Email = user.Email!,
                Token = encodedToken,
                NewPassword = "NewPassword123!"
            });

            Assert.False(result.IsSuccess);
            // IdentityError.Description всегда на английском и не подлежит локализации —
            // наружу должно уйти наше сообщение в едином стиле, а не "Invalid token." как есть.
            Assert.Contains("Ссылка для сброса пароля недействительна", result.Error);
        }

        [Fact]
        public async Task ResetPasswordAsync_MalformedToken_FailsWithoutCallingIdentity()
        {
            var user = TestData.User();
            _userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);

            var result = await CreateSut().ResetPasswordAsync(new ResetPasswordRequest
            {
                Email = user.Email!,
                Token = "not-valid-base64url!!!",
                NewPassword = "NewPassword123!"
            });

            Assert.False(result.IsSuccess);
            _userManager.Verify(
                m => m.ResetPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }
    }
}
