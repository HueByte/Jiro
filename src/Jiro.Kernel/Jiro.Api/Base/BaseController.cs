using Microsoft.AspNetCore.Mvc;

namespace Jiro.Api.Base;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseController : Controller { }