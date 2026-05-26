using System.ComponentModel.DataAnnotations;

namespace Kista.Benchmarks.Models;

public sealed class EfBenchPerson {
    [Key]
    public int Id { get; set; }

    [MaxLength(128)]
    public string FirstName { get; set; } = String.Empty;

    [MaxLength(128)]
    public string LastName { get; set; } = String.Empty;

    [MaxLength(256)]
    public string? Email { get; set; }
}