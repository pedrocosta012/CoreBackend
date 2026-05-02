namespace CoreBackend.Categories;

public sealed record CategoryRequest(string Name, string Type, string? CompanyId = null);

