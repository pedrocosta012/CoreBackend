using System.Data;
using System.Text.Json;
using Dapper;

namespace CoreBackend.Customers;

internal static class CustomerEndpoints
{
    private const string SelectColumns =
        """
        id, name, COALESCE(document, '') AS document, COALESCE(email, '') AS email,
        COALESCE(phone, '') AS phone, segment, status,
        CAST(totalSpent AS REAL) AS totalSpent,
        ordersCount,
        COALESCE(lastPurchaseDate, '') AS lastPurchaseDate,
        loyaltyPoints,
        COALESCE(address, '') AS address, COALESCE(city, '') AS city,
        COALESCE(state, '') AS state, COALESCE(notes, '') AS notes,
        COALESCE(tags, '[]') AS tags, createdAt
        """;

    public static void MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/customers", async (CreateCustomerRequest request, IDbConnection db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(new { error = "Name is required." });
            }

            var id = Guid.NewGuid().ToString();
            var tagsJson = request.Tags is not null ? JsonSerializer.Serialize(request.Tags) : "[]";

            await db.ExecuteAsync(
                """
                INSERT INTO customer (id, name, document, email, phone, segment, status, totalSpent, ordersCount, lastPurchaseDate, loyaltyPoints, address, city, state, notes, tags)
                VALUES (@Id, @Name, @Document, @Email, @Phone, @Segment, @Status, @TotalSpent, @OrdersCount, @LastPurchaseDate, @LoyaltyPoints, @Address, @City, @State, @Notes, @Tags)
                """,
                new
                {
                    Id = id,
                    request.Name,
                    Document = request.Document ?? "",
                    Email = request.Email ?? "",
                    Phone = request.Phone ?? "",
                    Segment = request.Segment ?? "new",
                    Status = request.Status ?? "active",
                    TotalSpent = request.TotalSpent ?? 0.0,
                    OrdersCount = request.OrdersCount ?? 0,
                    LastPurchaseDate = request.LastPurchaseDate ?? "",
                    LoyaltyPoints = request.LoyaltyPoints ?? 0,
                    Address = request.Address ?? "",
                    City = request.City ?? "",
                    State = request.State ?? "",
                    Notes = request.Notes ?? "",
                    Tags = tagsJson
                });

            return Results.Created($"/customers/{id}", ToResponse(id, request.Name, request.Document ?? "", request.Email ?? "",
                request.Phone ?? "", request.Segment ?? "new", request.Status ?? "active",
                request.TotalSpent ?? 0, request.OrdersCount ?? 0, request.LastPurchaseDate ?? "",
                request.LoyaltyPoints ?? 0, request.Address ?? "", request.City ?? "", request.State ?? "",
                request.Notes ?? "", tagsJson, ""));
        });

        app.MapGet("/customers", async (IDbConnection db) =>
        {
            var rows = await db.QueryAsync<CustomerRow>(
                $"SELECT {SelectColumns} FROM customer WHERE deletedAt IS NULL ORDER BY createdAt DESC");

            var result = rows.Select(r => ToResponse(r.Id, r.Name, r.Document, r.Email, r.Phone,
                r.Segment, r.Status, r.TotalSpent, r.OrdersCount, r.LastPurchaseDate,
                r.LoyaltyPoints, r.Address, r.City, r.State, r.Notes, r.Tags, r.CreatedAt)).ToList();

            return Results.Ok(result);
        });

        app.MapGet("/customers/{id}", async (string id, IDbConnection db) =>
        {
            var row = await db.QuerySingleOrDefaultAsync<CustomerRow>(
                $"SELECT {SelectColumns} FROM customer WHERE id = @Id AND deletedAt IS NULL",
                new { Id = id });

            if (row is null) return Results.NotFound();

            return Results.Ok(ToResponse(row.Id, row.Name, row.Document, row.Email, row.Phone,
                row.Segment, row.Status, row.TotalSpent, row.OrdersCount, row.LastPurchaseDate,
                row.LoyaltyPoints, row.Address, row.City, row.State, row.Notes, row.Tags, row.CreatedAt));
        });

        app.MapPut("/customers/{id}", async (string id, UpdateCustomerRequest request, IDbConnection db) =>
        {
            var existing = await db.QuerySingleOrDefaultAsync<CustomerRow>(
                $"SELECT {SelectColumns} FROM customer WHERE id = @Id AND deletedAt IS NULL",
                new { Id = id });

            if (existing is null) return Results.NotFound();

            var name = request.Name ?? existing.Name;
            var document = request.Document ?? existing.Document;
            var email = request.Email ?? existing.Email;
            var phone = request.Phone ?? existing.Phone;
            var segment = request.Segment ?? existing.Segment;
            var status = request.Status ?? existing.Status;
            var totalSpent = request.TotalSpent ?? existing.TotalSpent;
            var ordersCount = request.OrdersCount ?? existing.OrdersCount;
            var lastPurchaseDate = request.LastPurchaseDate ?? existing.LastPurchaseDate;
            var loyaltyPoints = request.LoyaltyPoints ?? existing.LoyaltyPoints;
            var address = request.Address ?? existing.Address;
            var city = request.City ?? existing.City;
            var state = request.State ?? existing.State;
            var notes = request.Notes ?? existing.Notes;
            var tagsJson = request.Tags is not null ? JsonSerializer.Serialize(request.Tags) : existing.Tags;

            await db.ExecuteAsync(
                """
                UPDATE customer
                SET name = @Name, document = @Document, email = @Email, phone = @Phone,
                    segment = @Segment, status = @Status, totalSpent = @TotalSpent,
                    ordersCount = @OrdersCount, lastPurchaseDate = @LastPurchaseDate,
                    loyaltyPoints = @LoyaltyPoints, address = @Address, city = @City,
                    state = @State, notes = @Notes, tags = @Tags,
                    updatedAt = CURRENT_TIMESTAMP
                WHERE id = @Id AND deletedAt IS NULL
                """,
                new
                {
                    Id = id, Name = name, Document = document, Email = email, Phone = phone,
                    Segment = segment, Status = status, TotalSpent = totalSpent,
                    OrdersCount = ordersCount, LastPurchaseDate = lastPurchaseDate,
                    LoyaltyPoints = loyaltyPoints, Address = address, City = city,
                    State = state, Notes = notes, Tags = tagsJson
                });

            return Results.Ok(ToResponse(id, name, document, email, phone, segment, status,
                totalSpent, ordersCount, lastPurchaseDate, loyaltyPoints, address, city, state,
                notes, tagsJson, existing.CreatedAt));
        });

        app.MapDelete("/customers/{id}", async (string id, IDbConnection db) =>
        {
            var rows = await db.ExecuteAsync(
                """
                UPDATE customer
                SET deletedAt = CURRENT_TIMESTAMP
                WHERE id = @Id AND deletedAt IS NULL
                """,
                new { Id = id });

            return rows > 0 ? Results.NoContent() : Results.NotFound();
        });
    }

    private static object ToResponse(string id, string name, string document, string email,
        string phone, string segment, string status, double totalSpent, long ordersCount,
        string lastPurchaseDate, long loyaltyPoints, string address, string city, string state,
        string notes, string tagsJson, string createdAt)
    {
        string[] tags;
        try { tags = JsonSerializer.Deserialize<string[]>(tagsJson) ?? []; }
        catch { tags = []; }

        return new
        {
            id, name, document, email, phone, segment, status,
            totalSpent, ordersCount, lastPurchaseDate,
            loyaltyPoints, address, city, state, notes, tags,
            dateJoined = createdAt
        };
    }
}
