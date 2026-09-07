using LMSFinal.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace LMSFinal.Tests.Common
{

    internal static class MockUserManager
    {
        public static Mock<UserManager<ApplicationUser>> Create()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();

            return new Mock<UserManager<ApplicationUser>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        }
    }
}
