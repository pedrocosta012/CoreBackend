using System.Data;
using Dapper;

namespace CoreBackend.Categories;

internal static class CategoryEndpoints
{
    public static void MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/categories", async (CategoryRequest request, IDbConnection db) =>
        {
            var id = Guid.NewGuid().ToString();
            await db.ExecuteAsync(
                "INSERT INTO category (id, name, type) VALUES (@Id, @Name, @Type)",
                new { Id = id, request.Name, request.Type });

            return Results.Created($"/categories/{id}", new { id, request.Name, request.Type });
        });

        app.MapGet("/categories", async (IDbConnection db) =>
        {
            var categories = await db.QueryAsync("SELECT id, name, type FROM category");
            return Results.Ok(categories);
        });

        app.MapGet("/categories/{id}", async (string id, IDbConnection db) =>
        {
            var category = await db.QuerySingleOrDefaultAsync(
                "SELECT id, name, type FROM category WHERE id = @Id",
                new { Id = id });

            return category is not null ? Results.Ok(category) : Results.NotFound();
        });

        app.MapPut("/categories/{id}", async (string id, CategoryRequest request, IDbConnection db) =>
        {
            var rows = await db.ExecuteAsync(
                "UPDATE category SET name = @Name, type = @Type WHERE id = @Id",
                new { Id = id, request.Name, request.Type });

            if (rows == 0)
            {
                return Results.NotFound();
            }

            return Results.Ok(new { id, request.Name, request.Type });
        });

        app.MapDelete("/categories/{id}", async (string id, IDbConnection db) =>
        {
            var rows = await db.ExecuteAsync("DELETE FROM category WHERE id = @Id", new { Id = id });
            return rows > 0 ? Results.NoContent() : Results.NotFound();
        });
    }
}

