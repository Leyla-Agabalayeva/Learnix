using LMSFinal.Application.Interfaces;
using LMSFinal.Contracts.DTOs.Auth;
using LMSFinal.Domain.Common;
using LMSFinal.Domain.Entities;
using LMSFinal.Domain.Enums;
using LMSFinal.Infrastructure.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
namespace LMSFinal.Infrastructure.Identity
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IEmailSender _emailSender;
        private readonly EmailSettings _emailSettings;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            IJwtTokenService jwtTokenService,
            IEmailSender emailSender,
            IOptions<EmailSettings> emailSettings)
        {
            _userManager = userManager;
            _jwtTokenService = jwtTokenService;
            _emailSender = emailSender;
            _emailSettings = emailSettings.Value;
        }

        public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
        {

            if (request.Role != UserRole.Instructor && request.Role != UserRole.Student)
            {
                return Result<AuthResponse>.Failure("Регистрация доступна только с ролью Instructor или Student.");
            }

            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser is not null)
            {
                return Result<AuthResponse>.Failure("Пользователь с таким email уже зарегистрирован.");
            }

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName
            };

            // Профессиональный профиль есть только у преподавателя; у студента поля остаются null,
            // даже если клиент прислал лишнее.
            if (request.Role == UserRole.Instructor)
            {
                user.Bio = Clean(request.Bio);
                user.ProfessionalTitle = Clean(request.ProfessionalTitle);
                user.Specialization = Clean(request.Specialization);
                user.YearsOfExperience = request.YearsOfExperience;
                user.EducationLevel = Clean(request.EducationLevel);
                user.Organization = Clean(request.Organization);
                user.ProfileUrl = Clean(request.ProfileUrl);
            }

            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(" ", createResult.Errors.Select(e => e.Description));
                return Result<AuthResponse>.Failure(errors);
            }

            var roleName = request.Role.ToString();
            var roleResult = await _userManager.AddToRoleAsync(user, roleName);
            if (!roleResult.Succeeded)
            {

                await _userManager.DeleteAsync(user);
                var errors = string.Join(" ", roleResult.Errors.Select(e => e.Description));
                return Result<AuthResponse>.Failure($"Не удалось назначить роль: {errors}");
            }

            var (token, expiresAt) = _jwtTokenService.GenerateToken(user, new[] { roleName });

            return Result<AuthResponse>.Success(new AuthResponse(
      token, expiresAt, user.Id, user.Email!, user.FirstName, user.LastName, new[] { roleName }, user.AvatarUrl));
        }

        public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user is null)
            {

                return Result<AuthResponse>.Failure("Неверный email или пароль.");
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!passwordValid)
            {
                return Result<AuthResponse>.Failure("Неверный email или пароль.");
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                return Result<AuthResponse>.Failure("Аккаунт заблокирован администратором.");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var (token, expiresAt) = _jwtTokenService.GenerateToken(user, roles);

            return Result<AuthResponse>.Success(new AuthResponse(
     token, expiresAt, user.Id, user.Email!, user.FirstName, user.LastName, roles.ToList(), user.AvatarUrl));
        }

        public async Task<Result<CurrentUserResponse>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
            {
                return Result<CurrentUserResponse>.Failure("Пользователь не найден.");
            }

            var roles = await _userManager.GetRolesAsync(user);

            return Result<CurrentUserResponse>.Success(MapProfile(user, roles));
        }

        public async Task<Result<CurrentUserResponse>> UpdateProfileAsync(
            Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
        {

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
            {
                return Result<CurrentUserResponse>.Failure("Пользователь не найден.");
            }

            user.FirstName = request.FirstName.Trim();
            user.LastName = request.LastName.Trim();

            user.Bio = Clean(request.Bio);
            user.AvatarUrl = Clean(request.AvatarUrl);

            // Данные преподавателя обновляем только у преподавателя.
            if (await _userManager.IsInRoleAsync(user, UserRole.Instructor.ToString()))
            {
                user.ProfessionalTitle = Clean(request.ProfessionalTitle);
                user.Specialization = Clean(request.Specialization);
                user.YearsOfExperience = request.YearsOfExperience;
                user.EducationLevel = Clean(request.EducationLevel);
                user.Organization = Clean(request.Organization);
                user.ProfileUrl = Clean(request.ProfileUrl);
            }

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return Result<CurrentUserResponse>.Failure(
                    string.Join("; ", result.Errors.Select(error => error.Description)));
            }

            var roles = await _userManager.GetRolesAsync(user);

            return Result<CurrentUserResponse>.Success(MapProfile(user, roles));
        }

        public async Task<Result<CurrentUserResponse>> UpdateAvatarAsync(
            Guid userId, string avatarUrl, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
            {
                return Result<CurrentUserResponse>.Failure("Пользователь не найден.");
            }

            user.AvatarUrl = avatarUrl;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return Result<CurrentUserResponse>.Failure(
                    string.Join("; ", result.Errors.Select(error => error.Description)));
            }

            var roles = await _userManager.GetRolesAsync(user);

            return Result<CurrentUserResponse>.Success(MapProfile(user, roles));
        }

        public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user is null)
            {

                return;
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);


            var encodedToken = Base64UrlEncode(token);

            var resetLink =
                $"{_emailSettings.ClientBaseUrl}/pages/reset-password.html" +
                $"?email={WebUtility.UrlEncode(user.Email)}&token={encodedToken}";

            var body = $"""
                <p>Здравствуйте, {WebUtility.HtmlEncode(user.FirstName)}!</p>
                <p>Вы (или кто-то другой) запросили сброс пароля на Learnix. Если это были не вы —
                просто проигнорируйте это письмо, пароль останется прежним.</p>
                <p><a href="{resetLink}">Придумать новый пароль</a></p>
                <p>Ссылка действительна ограниченное время и работает только один раз.</p>
                """;

            await _emailSender.SendAsync(user.Email!, "Сброс пароля — Learnix", body, cancellationToken);
        }

        public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user is null)
            {

                return Result.Failure("Ссылка для сброса пароля недействительна или устарела.");
            }

            string token;
            try
            {
                token = Base64UrlDecode(request.Token);
            }
            catch (FormatException)
            {
                return Result.Failure("Ссылка для сброса пароля повреждена — запросите новую.");
            }

            var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);

            if (!result.Succeeded)
            {
                // IdentityError.Description всегда на английском ("Invalid token." и т.п.) —
                // это сообщения ASP.NET Identity, а не наши, и локализации не подлежат. Показываем
                // единое сообщение в стиле остальных веток этого метода вместо английского текста
                // посреди русского интерфейса.
                return Result.Failure("Ссылка для сброса пароля недействительна или устарела.");
            }

            return Result.Success();
        }


        private static string Base64UrlEncode(string value) =>
            Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');

        private static string Base64UrlDecode(string value)
        {
            var base64 = value.Replace('-', '+').Replace('_', '/');
            var padding = base64.Length % 4;
            if (padding > 0)
            {
                base64 += new string('=', 4 - padding);
            }

            return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
        }

        private static string? Clean(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static CurrentUserResponse MapProfile(ApplicationUser user, IEnumerable<string> roles) =>
            new(user.Id, user.Email!, user.FirstName, user.LastName, roles.ToList(),
                user.Bio, user.AvatarUrl, user.CreatedAt,
                user.ProfessionalTitle, user.Specialization, user.YearsOfExperience,
                user.EducationLevel, user.Organization, user.ProfileUrl);
    }

}
