using System.Data;
using Dapper;
using Microsoft.AspNetCore.Mvc;

namespace CoreBackend.Categories;

[ApiController]
[Route("categories")]
public sealed class CategoryController : ControllerBase
{
    [HttpPost]
    public async Task<IResult> Create([FromBody] CategoryRequest request, [FromServices] IDbConnection db)
    {
        var id = Guid.NewGuid().ToString();
        var companyId = string.IsNullOrWhiteSpace(request.CompanyId)
            ? Guid.NewGuid().ToString()
            : request.CompanyId;

        await db.ExecuteAsync(
            "INSERT INTO category (id, companyId, name, type) VALUES (@Id, @CompanyId, @Name, @Type)",
            new { Id = id, CompanyId = companyId, request.Name, request.Type });

        return Results.Created($"/categories/{id}", new { id, request.Name, request.Type });
    }

    [HttpGet]
    public async Task<IResult> List([FromServices] IDbConnection db)
    {
        var categories = await db.QueryAsync("SELECT id, companyId, name, type FROM category");
        return Results.Ok(categories);
    }

    [HttpGet("{id}")]
    public async Task<IResult> GetById(string id, [FromServices] IDbConnection db)
    {
        var category = await db.QuerySingleOrDefaultAsync(
            "SELECT id, companyId, name, type FROM category WHERE id = @Id",
            new { Id = id });

        return category is not null ? Results.Ok(category) : Results.NotFound();
    }

    [HttpPut("{id}")]
    public async Task<IResult> Update(string id, [FromBody] CategoryRequest request, [FromServices] IDbConnection db)
    {
        var rows = await db.ExecuteAsync(
            "UPDATE category SET name = @Name, type = @Type WHERE id = @Id",
            new { Id = id, request.Name, request.Type });

        if (rows == 0)
        {
            return Results.NotFound();
        }

        return Results.Ok(new { id, request.Name, request.Type });
    }

    [HttpDelete("{id}")]
    public async Task<IResult> Delete(string id, [FromServices] IDbConnection db)
    {
        var rows = await db.ExecuteAsync("DELETE FROM category WHERE id = @Id", new { Id = id });
        return rows > 0 ? Results.NoContent() : Results.NotFound();
    }
}
