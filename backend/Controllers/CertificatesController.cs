using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLearning.Api.Data;
using SmartLearning.Api.Dtos;
using SmartLearning.Api.Models;

namespace SmartLearning.Api.Controllers
{
    // Public certificate check: anybody (for example an employer) can confirm that
    // a certificate is real by opening /verify/SL-5-12-34 on the website.
    [Route("api/certificates")]
    public class CertificatesController : BaseApiController
    {
        private readonly AppDbContext _db;

        public CertificatesController(AppDbContext db)
        {
            _db = db;
        }

        // GET api/certificates/SL-5-12-34
        // The number is "SL-" + course id + "-" + user id + "-" + enrollment id.
        [HttpGet("{number}")]
        public async Task<ActionResult<CertificateCheckDto>> Check(string number)
        {
            CertificateCheckDto notFound = new CertificateCheckDto { IsValid = false, CertificateNumber = number };

            // Split "SL-5-12-34" into its three numbers.
            string[] parts = (number ?? "").Trim().ToUpper().Split('-');
            if (parts.Length != 4 || parts[0] != "SL")
            {
                return Ok(notFound);
            }

            int courseId;
            int userId;
            int enrollmentId;
            if (!int.TryParse(parts[1], out courseId) || !int.TryParse(parts[2], out userId) || !int.TryParse(parts[3], out enrollmentId))
            {
                return Ok(notFound);
            }

            Enrollment enrollment = await _db.Enrollments
                .Include(e => e.User)
                .Include(e => e.Course)
                    .ThenInclude(c => c.Instructor)
                .FirstOrDefaultAsync(e => e.Id == enrollmentId && e.UserId == userId && e.CourseId == courseId);

            // Only completed courses have a certificate.
            if (enrollment == null || enrollment.CompletedAt == null)
            {
                return Ok(notFound);
            }

            int totalSeconds = await _db.Lectures
                .Where(l => l.Section.CourseId == courseId)
                .SumAsync(l => l.DurationSeconds);

            return Ok(new CertificateCheckDto
            {
                IsValid = true,
                CertificateNumber = "SL-" + courseId + "-" + userId + "-" + enrollmentId,
                StudentName = enrollment.User.FullName,
                CourseTitle = enrollment.Course.Title,
                CourseId = courseId,
                InstructorName = enrollment.Course.Instructor.FullName,
                TotalDurationSeconds = totalSeconds,
                CompletedAt = enrollment.CompletedAt
            });
        }
    }
}
