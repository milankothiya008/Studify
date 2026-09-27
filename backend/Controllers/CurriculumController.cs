using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLearning.Api.Data;
using SmartLearning.Api.Dtos;
using SmartLearning.Api.Models;
using SmartLearning.Api.Services;

namespace SmartLearning.Api.Controllers
{
    // Sections and lectures of a course (the "Curriculum" tab of the course editor).
    [Authorize(Roles = Roles.Instructor + "," + Roles.Admin)]
    [Route("api/instructor")]
    public class CurriculumController : BaseApiController
    {
        private readonly AppDbContext _db;
        private readonly FileStorageService _fileStorage;

        public CurriculumController(AppDbContext db, FileStorageService fileStorage)
        {
            _db = db;
            _fileStorage = fileStorage;
        }

        // ===================== Sections =====================

        // POST api/instructor/courses/5/sections
        [HttpPost("courses/{courseId}/sections")]
        public async Task<ActionResult> AddSection(int courseId, SectionSaveRequest request)
        {
            Course course = await _db.Courses.FindAsync(courseId);
            if (course == null || !IsMine(course))
            {
                return ErrorMessage(404, "Course not found.");
            }

            // New sections go to the end.
            int lastOrder = 0;
            bool hasSections = await _db.Sections.AnyAsync(s => s.CourseId == courseId);
            if (hasSections)
            {
                lastOrder = await _db.Sections
                    .Where(s => s.CourseId == courseId)
                    .MaxAsync(s => s.OrderIndex);
            }

            Section section = new Section
            {
                CourseId = courseId,
                Title = request.Title.Trim(),
                OrderIndex = lastOrder + 1
            };

            _db.Sections.Add(section);
            course.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new { id = section.Id });
        }

        // PUT api/instructor/sections/7
        [HttpPut("sections/{id}")]
        public async Task<ActionResult> UpdateSection(int id, SectionSaveRequest request)
        {
            Section section = await FindMySectionAsync(id);
            if (section == null)
            {
                return ErrorMessage(404, "Section not found.");
            }

            section.Title = request.Title.Trim();
            await _db.SaveChangesAsync();
            return Ok(new { message = "Section saved." });
        }

        // DELETE api/instructor/sections/7  (also deletes its lectures)
        [HttpDelete("sections/{id}")]
        public async Task<ActionResult> DeleteSection(int id)
        {
            Section section = await FindMySectionAsync(id);
            if (section == null)
            {
                return ErrorMessage(404, "Section not found.");
            }

            List<Lecture> lectures = await _db.Lectures.Where(l => l.SectionId == id).ToListAsync();
            foreach (Lecture lecture in lectures)
            {
                await _fileStorage.DeleteFileAsync(lecture.VideoPublicId, true);
            }

            _db.Sections.Remove(section);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // POST api/instructor/sections/7/move?direction=up   (or down)
        [HttpPost("sections/{id}/move")]
        public async Task<ActionResult> MoveSection(int id, string direction)
        {
            Section section = await FindMySectionAsync(id);
            if (section == null)
            {
                return ErrorMessage(404, "Section not found.");
            }

            List<Section> sections = await _db.Sections
                .Where(s => s.CourseId == section.CourseId)
                .OrderBy(s => s.OrderIndex)
                .ToListAsync();

            int index = sections.FindIndex(s => s.Id == id);
            int otherIndex = direction == "up" ? index - 1 : index + 1;

            if (otherIndex >= 0 && otherIndex < sections.Count)
            {
                // Swap the two sections, then number all sections again 1, 2, 3 ...
                Section other = sections[otherIndex];
                sections[otherIndex] = section;
                sections[index] = other;

                for (int i = 0; i < sections.Count; i++)
                {
                    sections[i].OrderIndex = i + 1;
                }
                await _db.SaveChangesAsync();
            }

            return Ok();
        }

        // ===================== Lectures =====================

        // POST api/instructor/sections/7/lectures  -> creates a lecture (the video is uploaded in a second step)
        [HttpPost("sections/{sectionId}/lectures")]
        public async Task<ActionResult> AddLecture(int sectionId, LectureSaveRequest request)
        {
            Section section = await FindMySectionAsync(sectionId);
            if (section == null)
            {
                return ErrorMessage(404, "Section not found.");
            }

            // New lectures go to the end of the section.
            int lastOrder = 0;
            bool hasLectures = await _db.Lectures.AnyAsync(l => l.SectionId == sectionId);
            if (hasLectures)
            {
                lastOrder = await _db.Lectures
                    .Where(l => l.SectionId == sectionId)
                    .MaxAsync(l => l.OrderIndex);
            }

            Lecture lecture = new Lecture
            {
                SectionId = sectionId,
                Title = request.Title.Trim(),
                Description = request.Description,
                IsFreePreview = request.IsFreePreview,
                OrderIndex = lastOrder + 1
            };

            _db.Lectures.Add(lecture);
            await _db.SaveChangesAsync();

            return Ok(new { id = lecture.Id });
        }

        // PUT api/instructor/lectures/9
        [HttpPut("lectures/{id}")]
        public async Task<ActionResult> UpdateLecture(int id, LectureSaveRequest request)
        {
            Lecture lecture = await FindMyLectureAsync(id);
            if (lecture == null)
            {
                return ErrorMessage(404, "Lecture not found.");
            }

            lecture.Title = request.Title.Trim();
            lecture.Description = request.Description;
            lecture.IsFreePreview = request.IsFreePreview;
            await _db.SaveChangesAsync();

            return Ok(new { message = "Lecture saved." });
        }

        // POST api/instructor/lectures/9/video   (form-data: file, durationSeconds)
        // durationSeconds is measured by the browser before uploading. Cloudinary also
        // tells us the duration, and when it does we use Cloudinary's value.
        [HttpPost("lectures/{id}/video")]
        public async Task<ActionResult> UploadLectureVideo(int id, IFormFile file, [FromForm] int durationSeconds)
        {
            Lecture lecture = await FindMyLectureAsync(id);
            if (lecture == null)
            {
                return ErrorMessage(404, "Lecture not found.");
            }

            try
            {
                UploadedFile uploaded = await _fileStorage.UploadVideoAsync(file);

                // Remove the old video so it does not use storage space.
                await _fileStorage.DeleteFileAsync(lecture.VideoPublicId, true);

                lecture.VideoUrl = uploaded.Url;
                lecture.VideoPublicId = uploaded.PublicId;
                lecture.DurationSeconds = uploaded.DurationSeconds > 0 ? uploaded.DurationSeconds : durationSeconds;
                await _db.SaveChangesAsync();

                return Ok(new { url = uploaded.Url, durationSeconds = lecture.DurationSeconds });
            }
            catch (Exception ex)
            {
                return ErrorMessage(400, ex.Message);
            }
        }

        // DELETE api/instructor/lectures/9
        [HttpDelete("lectures/{id}")]
        public async Task<ActionResult> DeleteLecture(int id)
        {
            Lecture lecture = await FindMyLectureAsync(id);
            if (lecture == null)
            {
                return ErrorMessage(404, "Lecture not found.");
            }

            await _fileStorage.DeleteFileAsync(lecture.VideoPublicId, true);

            _db.Lectures.Remove(lecture);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // POST api/instructor/lectures/9/move?direction=up   (or down)
        [HttpPost("lectures/{id}/move")]
        public async Task<ActionResult> MoveLecture(int id, string direction)
        {
            Lecture lecture = await FindMyLectureAsync(id);
            if (lecture == null)
            {
                return ErrorMessage(404, "Lecture not found.");
            }

            List<Lecture> lectures = await _db.Lectures
                .Where(l => l.SectionId == lecture.SectionId)
                .OrderBy(l => l.OrderIndex)
                .ToListAsync();

            int index = lectures.FindIndex(l => l.Id == id);
            int otherIndex = direction == "up" ? index - 1 : index + 1;

            if (otherIndex >= 0 && otherIndex < lectures.Count)
            {
                Lecture other = lectures[otherIndex];
                lectures[otherIndex] = lecture;
                lectures[index] = other;

                for (int i = 0; i < lectures.Count; i++)
                {
                    lectures[i].OrderIndex = i + 1;
                }
                await _db.SaveChangesAsync();
            }

            return Ok();
        }

        // ---------- helpers ----------

        private bool IsMine(Course course)
        {
            return course.InstructorId == GetUserId() || GetUserRole() == Roles.Admin;
        }

        private async Task<Section> FindMySectionAsync(int sectionId)
        {
            Section section = await _db.Sections
                .Include(s => s.Course)
                .FirstOrDefaultAsync(s => s.Id == sectionId);

            if (section == null || !IsMine(section.Course))
            {
                return null;
            }
            return section;
        }

        private async Task<Lecture> FindMyLectureAsync(int lectureId)
        {
            Lecture lecture = await _db.Lectures
                .Include(l => l.Section)
                    .ThenInclude(s => s.Course)
                .FirstOrDefaultAsync(l => l.Id == lectureId);

            if (lecture == null || !IsMine(lecture.Section.Course))
            {
                return null;
            }
            return lecture;
        }
    }
}
