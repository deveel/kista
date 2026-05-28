using System.ComponentModel.DataAnnotations;

namespace Kista;

public class UserOwnedEntity : IHaveOwner<string>
{
    [Key]
    public string Id { get; set; } = string.Empty;

    [DataOwner]
    public string OwnerId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    string IHaveOwner<string>.Owner => OwnerId;

    void IHaveOwner<string>.SetOwner(string owner) => OwnerId = owner;
}

public class UserOwnedEntityWithExplicitOwner : IHaveOwner<Guid>
{
    [Key]
    public Guid Id { get; set; }

    public Guid EntityOwnerId { get; set; }

    public string Title { get; set; } = string.Empty;

    Guid IHaveOwner<Guid>.Owner => EntityOwnerId;

    void IHaveOwner<Guid>.SetOwner(Guid owner) => EntityOwnerId = owner;
}
