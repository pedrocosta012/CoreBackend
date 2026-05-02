namespace CoreBackend.Customers;

public sealed record CreateCustomerRequest(
    string Name,
    string? Document,
    string? Email,
    string? Phone,
    string? Segment,
    string? Status,
    double? TotalSpent,
    int? OrdersCount,
    string? LastPurchaseDate,
    int? LoyaltyPoints,
    string? Address,
    string? City,
    string? State,
    string? Notes,
    string[]? Tags);

public sealed record UpdateCustomerRequest(
    string? Name,
    string? Document,
    string? Email,
    string? Phone,
    string? Segment,
    string? Status,
    double? TotalSpent,
    int? OrdersCount,
    string? LastPurchaseDate,
    int? LoyaltyPoints,
    string? Address,
    string? City,
    string? State,
    string? Notes,
    string[]? Tags);

internal sealed record CustomerRow(
    string Id,
    string Name,
    string Document,
    string Email,
    string Phone,
    string Segment,
    string Status,
    double TotalSpent,
    long OrdersCount,
    string LastPurchaseDate,
    long LoyaltyPoints,
    string Address,
    string City,
    string State,
    string Notes,
    string Tags,
    string CreatedAt);
