using System.Data;
using Dapper;

namespace CoreBackend.Companies;

internal static class CompanyEndpoints
{
    public static void MapCompanyEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/companies", async (IDbConnection db) =>
        {
            var companies = await db.QueryAsync(
                """
                SELECT id, name, officeType, COALESCE(taxId, '') AS taxId
                FROM company
                WHERE deletedAt IS NULL
                ORDER BY name
                """);

            return Results.Ok(companies);
        });

        app.MapGet("/companies/{id}", async (string id, IDbConnection db) =>
        {
            var company = await db.QuerySingleOrDefaultAsync(
                """
                SELECT id, name, officeType, COALESCE(taxId, '') AS taxId
                FROM company
                WHERE id = @Id AND deletedAt IS NULL
                """,
                new { Id = id });

            return company is not null ? Results.Ok(company) : Results.NotFound();
        });
    }
}
