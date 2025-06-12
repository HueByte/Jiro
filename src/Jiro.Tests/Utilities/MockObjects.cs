using Jiro.Core.Abstraction;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MockQueryable.Moq;

using Moq;

namespace Jiro.Tests.Utilities;

public static class MockObjects
{
	public static Mock<UserManager<TIdentityUser>> GetUserManagerMock<TIdentityUser> () where TIdentityUser : IdentityUser
	{
		return new Mock<UserManager<TIdentityUser>>(
				new Mock<IUserStore<TIdentityUser>>().Object,
				new Mock<IOptions<IdentityOptions>>().Object,
				new Mock<IPasswordHasher<TIdentityUser>>().Object,
				new IUserValidator<TIdentityUser>[0],
				new IPasswordValidator<TIdentityUser>[0],
				new Mock<ILookupNormalizer>().Object,
				new Mock<IdentityErrorDescriber>().Object,
				new Mock<IServiceProvider>().Object,
				new Mock<ILogger<UserManager<TIdentityUser>>>().Object);
	}

	public static Mock<RoleManager<TIdentityRole>> GetRoleManagerMock<TIdentityRole> () where TIdentityRole : IdentityRole
	{
		return new Mock<RoleManager<TIdentityRole>>(
				new Mock<IRoleStore<TIdentityRole>>().Object,
				new IRoleValidator<TIdentityRole>[0],
				new Mock<ILookupNormalizer>().Object,
				new Mock<IdentityErrorDescriber>().Object,
				new Mock<ILogger<RoleManager<TIdentityRole>>>().Object);
	}

	public static Mock<TRepository> CreateMockRepository<TRepository, TKey, TEntity> (List<TEntity>? entries = null)
		where TRepository : class, IRepository<TKey, TEntity>
		where TKey : IConvertible
		where TEntity : DbModel<TKey>
	{
		var mock = new Mock<TRepository>();

		entries ??= new List<TEntity>();
		mock.Setup(repo => repo.AsQueryable()).Returns(entries.BuildMock());

		return mock;
	}

	public static Mock<SignInManager<TIdentityUser>> GetSignInManagerMock<TIdentityUser> () where TIdentityUser : IdentityUser
	{
		return new Mock<SignInManager<TIdentityUser>>(
			GetUserManagerMock<TIdentityUser>().Object,
			/* IHttpContextAccessor contextAccessor */Mock.Of<IHttpContextAccessor>(),
			/* IUserClaimsPrincipalFactory<TUser> claimsFactory */Mock.Of<IUserClaimsPrincipalFactory<TIdentityUser>>(),
			/* IOptions<IdentityOptions> optionsAccessor */null,
			/* ILogger<SignInManager<TUser>> logger */null,
			/* IAuthenticationSchemeProvider schemes */null,
			/* IUserConfirmation<TUser> confirmation */null);
	}

	public static Mock<DbSet<T>> GetMockDbSet<T> (IEnumerable<T> data) where T : class
	{
		return data.AsQueryable().BuildMockDbSet();
	}
}
