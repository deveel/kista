using System.ComponentModel.DataAnnotations;

namespace Kista;

public class Person : IPerson<string>, IPerson {
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    [Key]
    public string? Id { get; set; }

    public string? Email { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public string? PhoneNumber { get; set; }

    public DateTimeOffset? CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public List<PersonRelationship> Relationships { get; set; } = new();

    IEnumerable<IRelationship> IPerson<string>.Relationships => Relationships;
}

public class PersonRelationship : IRelationship {
    public string Type { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;
}
