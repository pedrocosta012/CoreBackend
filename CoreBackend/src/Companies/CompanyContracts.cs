namespace CoreBackend.Companies;

internal sealed record CreateCompanyRequest(string Name, string OfficeType, string? TaxId);

internal sealed record CompanyRow(string Id, string Name, string OfficeType, string TaxId, string CreatedAt);
