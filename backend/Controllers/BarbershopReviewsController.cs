using System.Security.Claims;
using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/Barbershops/{barbershopId:int}/reviews")]
public class BarbershopReviewsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public BarbershopReviewsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ReviewPublicDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ReviewPublicDto>>> GetApproved(int barbershopId,
        CancellationToken cancellationToken)
    {
        if (!await _db.Barbershops.AnyAsync(s => s.Id == barbershopId, cancellationToken))
            return NotFound();

        var rows = await _db.Reviews.AsNoTracking()
            .Where(r => r.BarbershopId == barbershopId && r.ModerationStatus == ReviewModerationStatus.Approved)
            .Include(r => r.Client)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var list = rows.Select(r => new ReviewPublicDto
        {
            Id = r.Id,
            Rating = r.Rating,
            Comment = r.Comment,
            AuthorDisplayName = r.Client.FullName ?? r.Client.Email ?? "Klient",
            CreatedAtUtc = r.CreatedAtUtc,
        }).ToList();
        return Ok(list);
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ReviewPublicDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReviewPublicDto>> Create(int barbershopId, [FromBody] CreateReviewRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);
        if (!User.IsInRole(RoleNames.Klient))
            return Forbid();

        if (!await _db.Barbershops.AnyAsync(s => s.Id == barbershopId, cancellationToken))
            return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var review = new Review
        {
            BarbershopId = barbershopId,
            ClientId = userId,
            Rating = request.Rating,
            Comment = request.Comment.Trim(),
            ModerationStatus = ReviewModerationStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow,
        };
        _db.Reviews.Add(review);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return BadRequest(new { error = "Nie udało się zapisać opinii (sprawdź unikalność / dane)." });
        }

        var client = await _db.Users.AsNoTracking().FirstAsync(u => u.Id == userId, cancellationToken);
        var dto = new ReviewPublicDto
        {
            Id = review.Id,
            Rating = review.Rating,
            Comment = review.Comment,
            AuthorDisplayName = client.FullName ?? client.Email ?? "Klient",
            CreatedAtUtc = review.CreatedAtUtc,
        };
        return Created($"/api/Barbershops/{barbershopId}/reviews", dto);
    }
}
