using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLearning.Api.Data;
using SmartLearning.Api.Dtos;
using SmartLearning.Api.Models;

namespace SmartLearning.Api.Controllers
{
    [Route("api/categories")]
    public class CategoriesController : BaseApiController
    {
        private readonly AppDbContext _db;

        public CategoriesController(AppDbContext db)
        {
            _db = db;
        }

        // GET api/categories  (anyone)
        [HttpGet]
        public async Task<ActionResult<List<CategoryDto>>> GetAll()
        {
            List<CategoryDto> categories = await _db.Categories
                .OrderBy(c => c.Name)
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    CourseCount = c.Courses.Count(course => course.IsPublished)
                })
                .ToListAsync();

            return Ok(categories);
        }

        // POST api/categories  (admin only)
        [Authorize(Roles = Roles.Admin)]
        [HttpPost]
        public async Task<ActionResult<CategoryDto>> Create(CategorySaveRequest request)
        {
            Category category = new Category { Name = request.Name.Trim() };
            _db.Categories.Add(category);
            await _db.SaveChangesAsync();

            return Ok(new CategoryDto { Id = category.Id, Name = category.Name });
        }

        // PUT api/categories/5  (admin only)
        [Authorize(Roles = Roles.Admin)]
        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, CategorySaveRequest request)
        {
            Category category = await _db.Categories.FindAsync(id);
            if (category == null)
            {
                return ErrorMessage(404, "Category not found.");
            }

            category.Name = request.Name.Trim();
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE api/categories/5  (admin only). Its courses keep existing without a category.
        [Authorize(Roles = Roles.Admin)]
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            Category category = await _db.Categories.FindAsync(id);
            if (category == null)
            {
                return ErrorMessage(404, "Category not found.");
            }

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
