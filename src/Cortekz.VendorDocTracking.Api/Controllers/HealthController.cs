using Microsoft.AspNetCore.Mvc;

namespace Cortekz.VendorDocTracking.Api.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    // Note: this must stay an instance method — ASP.NET Core's controller action
    // discovery skips static methods entirely, so a static action registers no route.
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "healthy" });
}
