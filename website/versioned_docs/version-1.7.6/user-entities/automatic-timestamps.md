# Automatic Timestamps

Entities can implement `IHaveTimeStamp` to have creation and update timestamps automatically set by the `EntityManager`:

```csharp
public interface IHaveTimeStamp
{
    DateTimeOffset? CreatedAtUtc { get; set; }
    DateTimeOffset? UpdatedAtUtc { get; set; }
}
```

## Example Entity

```csharp
public class Article : IHaveOwner<string>, IHaveTimeStamp
{
    public string Id { get; set; }

    [DataOwner]
    public string OwnerId { get; set; }

    public DateTimeOffset? CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public string Title { get; set; }
    public string Content { get; set; }

    string IHaveOwner<string>.Owner => OwnerId;
    void IHaveOwner<string>.SetOwner(string owner) => OwnerId = owner;
}
```

## How It Works

When using `EntityManager<TEntity, TKey>`:

- **On Add:** `CreatedAtUtc` is automatically set to `ISystemTime.UtcNow` when an entity implementing `IHaveTimeStamp` is added
- **On Update:** `UpdatedAtUtc` is automatically set to `ISystemTime.UtcNow` when the entity is updated

```csharp
// Adding - CreatedAtUtc is set automatically
var article = await manager.AddAsync(new Article {
    Title = "My Article",
    Content = "Content here"
});

Console.WriteLine(article.CreatedAtUtc); // 2025-01-15T10:30:00Z

// Updating - UpdatedAtUtc is set automatically
article.Title = "Updated Title";
article = await manager.UpdateAsync(article);

Console.WriteLine(article.UpdatedAtUtc); // 2025-01-15T11:45:00Z
```

> Timestamps are only set if an `ISystemTime` service is registered in the DI container. This is typically done automatically in ASP.NET Core applications via `AddSystemTime()`.
