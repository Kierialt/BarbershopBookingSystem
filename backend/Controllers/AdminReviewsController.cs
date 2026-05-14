using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/admin/reviews")]
[Authorize(Roles = RoleNames.Administrator)]
public class AdminReviewsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AdminReviewsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("pending")]
    [ProducesResponseType(typeof(IReadOnlyList<ReviewPendingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ReviewPendingDto>>> GetPending(CancellationToken cancellationToken)
    {
        var rows = await _db.Reviews.AsNoTracking()
            .Where(r => r.ModerationStatus == ReviewModerationStatus.Pending)
            .Include(r => r.Barbershop)
            .Include(r => r.Client)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var list = rows.Select(r => new ReviewPendingDto
        {
            Id = r.Id,
            BarbershopId = r.BarbershopId,
            BarbershopName = r.Barbershop.Name,
            ClientEmail = r.Client.Email ?? string.Empty,
            ClientFullName = r.Client.FullName,
            Rating = r.Rating,
            Comment = r.Comment,
            ModerationStatus = r.ModerationStatus.ToString(),
            CreatedAtUtc = r.CreatedAtUtc,
        }).ToList();
        return Ok(list);
    }

    [HttpPatch("{id:int}/moderate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Moderate(int id, [FromBody] ReviewModerateRequest request,
        CancellationToken cancellationToken)
    {
        var review = await _db.Reviews.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (review is null)
            return NotFound();

        review.ModerationStatus = request.Approve
            ? ReviewModerationStatus.Approved
            : ReviewModerationStatus.Rejected;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
