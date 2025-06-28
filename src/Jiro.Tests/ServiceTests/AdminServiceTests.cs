using Jiro.Core.Services.Admin;

namespace Jiro.Tests.ServiceTests;

public class AdminServiceTests
{
	private readonly AdminService _adminService;

	public AdminServiceTests()
	{
		_adminService = new AdminService();
	}

	[Fact]
	public void Constructor_ShouldCreateInstance()
	{
		// Arrange & Act
		var service = new AdminService();

		// Assert
		Assert.NotNull(service);
	}

	[Fact]
	public void AdminService_ShouldBeInstantiable()
	{
		// Arrange & Act & Assert
		Assert.NotNull(_adminService);
	}
}
