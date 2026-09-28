using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLearning.Api.Data;
using SmartLearning.Api.Dtos;
using SmartLearning.Api.Models;
using SmartLearning.Api.Services;

namespace SmartLearning.Api.Controllers
{
    // Private notes a student writes while watching, saved with the second of the video.
    // Nobody else can see them.
    [Authorize]
    [Route("api")]
    public class NotesController : BaseApiController
    {
        private readonly AppDbContext _db;
        private readonly CourseAccessService _accessService;

        public NotesController(AppDbContext db, CourseAccessService accessService)
        {
            _db = db;
            _accessService = accessService;
        }

        // GET api/courses/5/notes  -> my notes in this course, in course order
        [HttpGet("courses/{courseId}/notes")]
        public async Task<ActionResult<List<NoteDto>>> GetNotes(int courseId)
        {
            int userId = GetUserId();

            List<NoteDto> notes = await _db.Notes
                .Where(n => n.UserId == userId && n.CourseId == courseId)
                .OrderBy(n => n.Lecture.Section.OrderIndex)
                .ThenBy(n => n.Lecture.OrderIndex)
                .ThenBy(n => n.Seconds)
                .Select(n => new NoteDto
                {
                    Id = n.Id,
                    LectureId = n.LectureId,
                    LectureTitle = n.Lecture.Title,
                    Seconds = n.Seconds,
                    Text = n.Text,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            return Ok(notes);
        }

        // POST api/lectures/9/notes   body: { "seconds": 83, "text": "Remember this!" }
        [HttpPost("lectures/{lectureId}/notes")]
        public async Task<ActionResult<NoteDto>> AddNote(int lectureId, NoteSaveRequest request)
        {
            int userId = GetUserId();

            Lecture lecture = await _db.Lectures
                .Include(l => l.Section)
                    .ThenInclude(s => s.Course)
                .FirstOrDefaultAsync(l => l.Id == lectureId);

            if (lecture == null)
            {
                return ErrorMessage(404, "Lecture not found.");
            }

            if (!await _accessService.CanWatchCourseAsync(userId, GetUserRole(), lecture.Section.Course))
            {
                return ErrorMessage(403, "You do not have access to this course.");
            }

            Note note = new Note
            {
                UserId = userId,
                CourseId = lecture.Section.CourseId,
                LectureId = lectureId,
                Seconds = request.Seconds,
                Text = request.Text.Trim(),
                CreatedAt = DateTime.UtcNow
            };
            _db.Notes.Add(note);
            await _db.SaveChangesAsync();

            return Ok(new NoteDto
            {
                Id = note.Id,
                LectureId = lectureId,
                LectureTitle = lecture.Title,
                Seconds = note.Seconds,
                Text = note.Text,
                CreatedAt = note.CreatedAt
            });
        }

        // PUT api/notes/4   body: { "seconds": 83, "text": "..." }
        [HttpPut("notes/{id}")]
        public async Task<ActionResult> UpdateNote(int id, NoteSaveRequest request)
        {
            Note note = await _db.Notes.FirstOrDefaultAsync(n => n.Id == id && n.UserId == GetUserId());
            if (note == null)
            {
                return ErrorMessage(404, "Note not found.");
            }

            note.Text = request.Text.Trim();
            await _db.SaveChangesAsync();
            return Ok(new { message = "Note saved." });
        }

        // DELETE api/notes/4
        [HttpDelete("notes/{id}")]
        public async Task<ActionResult> DeleteNote(int id)
        {
            Note note = await _db.Notes.FirstOrDefaultAsync(n => n.Id == id && n.UserId == GetUserId());
            if (note == null)
            {
                return ErrorMessage(404, "Note not found.");
            }

            _db.Notes.Remove(note);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
