using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLearning.Api.Data;
using SmartLearning.Api.Dtos;

namespace SmartLearning.Api.Controllers
{
    // The bell icon: my notifications.
    [Authorize]
    [Route("api/notifications")]
    public class NotificationsController : BaseApiController
    {
        private readonly AppDbContext _db;

        public NotificationsController(AppDbContext db)
        {
            _db = db;
        }

        // GET api/notifications  -> the 30 newest + how many are unread
        [HttpGet]
        public async Task<ActionResult<NotificationListDto>> GetNotifications()
        {
            int userId = GetUserId();

            List<NotificationDto> items = await _db.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(30)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Link = n.Link,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            int unread = await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

            return Ok(new NotificationListDto { UnreadCount = unread, Items = items });
        }

        // POST api/notifications/4/read
        [HttpPost("{id}/read")]
        public async Task<ActionResult> MarkRead(int id)
        {
            await _db.Notifications
                .Where(n => n.Id == id && n.UserId == GetUserId())
                .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.IsRead, true));
            return NoContent();
        }

        // POST api/notifications/read-all
        [HttpPost("read-all")]
        public async Task<ActionResult> MarkAllRead()
        {
            await _db.Notifications
                .Where(n => n.UserId == GetUserId() && !n.IsRead)
                .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.IsRead, true));
            return NoContent();
        }
    }
}
