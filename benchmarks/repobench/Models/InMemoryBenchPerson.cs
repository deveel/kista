using System.ComponentModel.DataAnnotations;

namespace Kista.Benchmarks.Models;

public sealed class InMemoryBenchPerson {
    [Key]
    public string? Id { get; set; }

    [MaxLength(128)]
    public string FirstName { get; set; } = String.Empty;

    [MaxLength(128)]
    public string LastName { get; set; } = String.Empty;

    [MaxLength(256)]
    public string? Email { get; set; }
}