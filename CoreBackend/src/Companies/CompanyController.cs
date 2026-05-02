using System.Data;
using Dapper;
using Microsoft.AspNetCore.Mvc;

namespace CoreBackend.Companies;

[ApiController]
[Route("companies")]
public sealed class CompanyController : ControllerBase
{
    [HttpGet]
    public async Task<IResult> List([FromServices] IDbConnection db)
    {
        var companies = await db.QueryAsync(
            """
            SELECT id, name, officeType, COALESCE(taxId, '') AS taxId
            FROM company
            WHERE deletedAt IS NULL
            ORDER BY name
            """);

        return Results.Ok(companies);
    }

    [HttpGet("{id}")]
    public async Task<IResult> GetById(string id, [FromServices] IDbConnection db)
    {
        var company = await db.QuerySingleOrDefaultAsync(
            """
            SELECT id, name, officeType, COALESCE(taxId, '') AS taxId
            FROM company
            WHERE id = @Id AND deletedAt IS NULL
            """,
            new { Id = id });

        return company is not null ? Results.Ok(company) : Results.NotFound();
    }
}
