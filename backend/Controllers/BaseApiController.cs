using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace SmartLearning.Api.Controllers
{
    // Every controller inherits from this class to get a few handy helpers.
    [ApiController]
    public abstract class BaseApiController : ControllerBase
    {
        // Id of the logged in user, read from the JWT token. Returns 0 when nobody is logged in.
        protected int GetUserId()
        {
            string id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(id))
            {
                return 0;
            }
            return int.Parse(id);
        }

        // "Student", "Instructor", "Admin" or null when nobody is logged in.
        protected string GetUserRole()
        {
            return User.FindFirstValue(ClaimTypes.Role);
        }

        // All error responses use the same shape: { "message": "..." }
        protected ActionResult ErrorMessage(int statusCode, string message)
        {
            return StatusCode(statusCode, new { message = message });
        }
    }
}
