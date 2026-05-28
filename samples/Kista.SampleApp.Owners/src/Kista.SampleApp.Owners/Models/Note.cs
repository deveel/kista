using System.ComponentModel.DataAnnotations;
namespace Kista.SampleApp.Owners.Models;

public class Note : Kista.IHaveOwner<string>
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    [DataOwner]
    public string OwnerId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    string Kista.IHaveOwner<string>.Owner => OwnerId;

    void Kista.IHaveOwner<string>.SetOwner(string owner) => OwnerId = owner;
}
