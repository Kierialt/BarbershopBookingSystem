using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace backend.Controllers;

/// <summary>
/// Endpointy pomocnicze wyłącznie na potrzeby developmentu (czyszczenie i ponowny seed danych demo).
/// Wymaga JWT użytkownika w roli <see cref="RoleNames.Administrator"/>.
/// </summary>
[ApiController]
[Route("api/dev/demo")]
[Authorize(Roles = RoleNames.Administrator)]
public class DevDemoController : ControllerBase
{
    /// <summary>
    /// Usuwa rezerwacje oraz całą domenę miasta/salonów demo i wstawia ją od nowa.
    /// Działa tylko gdy <c>ASPNETCORE_ENVIRONMENT=Development</c> oraz <c>Demo:AllowReseedEndpoint=true</c>.
    /// W Swaggerze: Authorize → token z <c>POST /api/auth/login</c> (konto z rolą Administrator).
    /// </summary>
    [HttpPost("reseed")]
    [ProducesResponseType(typeof(ReseedDemoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReseedDemoResponse>> ReseedDemo(
        [FromServices] ApplicationDbContext db,
        [FromServices] IWebHostEnvironment env,
        [FromServices] IConfiguration configuration,
        [FromServices] UserManager<ApplicationUser> userManager,
        [FromServices] ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        if (!env.IsDevelopment())
            return NotFound();

        if (!configuration.GetValue("Demo:AllowReseedEndpoint", false))
        {
            return Problem(
                title: "Reseed wyłączony",
                detail: "Ustaw Demo:AllowReseedEndpoint na true (np. w appsettings.Development.json).",
                statusCode: StatusCodes.Status403Forbidden);
        }

        await BarbershopDemoSeeder.ReseedDemoDomainAsync(db, cancellationToken);

        await DevStaffAccountsSeeder.EnsureAsync(db, userManager, configuration,
            loggerFactory.CreateLogger("DevStaffReseed"), cancellationToken);

        var cities = await db.Cities.CountAsync(cancellationToken);
        var shops = await db.Barbershops.CountAsync(cancellationToken);
        var barbers = await db.Barbers.CountAsync(cancellationToken);
        var services = await db.Services.CountAsync(cancellationToken);

        return Ok(new ReseedDemoResponse(
            "Dane demo zostały usunięte i wstawione ponownie.",
            cities,
            shops,
            barbers,
            services));
    }
}

public sealed record ReseedDemoResponse(
    string Message,
    int Cities,
    int Barbershops,
    int Barbers,
    int Services);
