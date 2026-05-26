namespace Kista.SampleApp.DTOs;

public record CreateContactRequest(
    string FirstName,
    string LastName,
    string? Email = null,
    string? Phone = null
);

public record UpdateContactRequest(
    string FirstName,
    string LastName,
    string? Email = null,
    string? Phone = null
);

public record ContactResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
