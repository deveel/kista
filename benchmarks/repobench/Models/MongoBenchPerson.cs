using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using MongoDB.Bson;

namespace Kista.Benchmarks.Models;

[Table("bench_people")]
public sealed class MongoBenchPerson {
    [Key, Column("_id")]
    public ObjectId Id { get; set; }

    [MaxLength(128)]
    [Column("first_name")]
    public string FirstName { get; set; } = String.Empty;

    [MaxLength(128)]
    [Column("last_name")]
    public string LastName { get; set; } = String.Empty;

    [MaxLength(256)]
    [Column("email")]
    public string? Email { get; set; }
}