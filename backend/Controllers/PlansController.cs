using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLearning.Api.Data;
using SmartLearning.Api.Dtos;
using SmartLearning.Api.Models;

namespace SmartLearning.Api.Controllers
{
    // Subscription plans (for example Monthly / Quarterly / Yearly).
    [Route("api/plans")]
    public class PlansController : BaseApiController
    {
        private readonly AppDbContext _db;

        public PlansController(AppDbContext db)
        {
            _db = db;
        }

        // GET api/plans            -> active plans (pricing page)
        // GET api/plans?all=true   -> every plan (admin page)
        [HttpGet]
        public async Task<ActionResult<List<PlanDto>>> GetPlans(bool all = false)
        {
            IQueryable<SubscriptionPlan> query = _db.SubscriptionPlans;

            bool showAll = all && GetUserRole() == Roles.Admin;
            if (!showAll)
            {
                query = query.Where(p => p.IsActive);
            }

            List<PlanDto> plans = await query
                .OrderBy(p => p.Price)
                .Select(p => new PlanDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    DurationDays = p.DurationDays,
                    IsActive = p.IsActive
                })
                .ToListAsync();

            return Ok(plans);
        }

        // POST api/plans  (admin only)
        [Authorize(Roles = Roles.Admin)]
        [HttpPost]
        public async Task<ActionResult> Create(PlanSaveRequest request)
        {
            SubscriptionPlan plan = new SubscriptionPlan();
            CopyRequestToPlan(request, plan);

            _db.SubscriptionPlans.Add(plan);
            await _db.SaveChangesAsync();

            return Ok(new { id = plan.Id });
        }

        // PUT api/plans/2  (admin only)
        [Authorize(Roles = Roles.Admin)]
        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, PlanSaveRequest request)
        {
            SubscriptionPlan plan = await _db.SubscriptionPlans.FindAsync(id);
            if (plan == null)
            {
                return ErrorMessage(404, "Plan not found.");
            }

            // Existing subscribers keep the end date they paid for.
            CopyRequestToPlan(request, plan);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Plan saved." });
        }

        // DELETE api/plans/2  (admin only). Plans that were already bought can only be deactivated.
        [Authorize(Roles = Roles.Admin)]
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            SubscriptionPlan plan = await _db.SubscriptionPlans.FindAsync(id);
            if (plan == null)
            {
                return ErrorMessage(404, "Plan not found.");
            }

            bool wasBought = await _db.UserSubscriptions.AnyAsync(s => s.PlanId == id);
            if (wasBought)
            {
                return ErrorMessage(400, "Students have bought this plan. Deactivate it instead of deleting it.");
            }

            _db.SubscriptionPlans.Remove(plan);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        private static void CopyRequestToPlan(PlanSaveRequest request, SubscriptionPlan plan)
        {
            plan.Name = request.Name.Trim();
            plan.Description = request.Description;
            plan.Price = request.Price;
            plan.DurationDays = request.DurationDays;
            plan.IsActive = request.IsActive;
        }
    }
}
