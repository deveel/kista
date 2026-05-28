using System.ComponentModel.DataAnnotations;
namespace Kista.SampleApp.Owners.Models;

public class TaskItem : Kista.IHaveOwner<string>
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;

    public bool IsCompleted { get; set; }

    public string Owner { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    string Kista.IHaveOwner<string>.Owner => Owner;

    void Kista.IHaveOwner<string>.SetOwner(string owner) => Owner = owner;
}
