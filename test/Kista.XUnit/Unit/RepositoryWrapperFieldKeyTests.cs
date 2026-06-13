using System.ComponentModel.DataAnnotations;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Core")]
[Trait("Feature", "Repository")]
public class RepositoryWrapperFieldKeyTests
{
    private static string NewId() => Guid.NewGuid().ToString("N");

    [Fact]
    public void Should_FindByFieldKey_When_EntityUsesFieldKey()
    {
        var id = NewId();
        var entity = new FieldKeyEntity { Id = id, Name = "Test" };
        var list = new List<FieldKeyEntity> { entity };
        var repo = list.AsRepository();

        var found = repo.FindAsync(id, TestContext.Current.CancellationToken).GetAwaiter().GetResult();

        Assert.NotNull(found);
        Assert.Equal(id, found.Id);
    }

    [Fact]
    public async Task Should_RemoveFieldKeyEntity_When_EntityExists()
    {
        var id = NewId();
        var entity = new FieldKeyEntity { Id = id, Name = "Test" };
        var list = new List<FieldKeyEntity> { entity };
        var repo = list.AsRepository();

        var removed = await repo.RemoveAsync(entity, TestContext.Current.CancellationToken);

        Assert.True(removed);
        Assert.Empty(list);
    }

    [Fact]
    public async Task Should_UpdateFieldKeyEntity_When_EntityExists()
    {
        var id = NewId();
        var entity = new FieldKeyEntity { Id = id, Name = "Original" };
        var list = new List<FieldKeyEntity> { entity };
        var repo = list.AsRepository();
        entity.Name = "Updated";

        var updated = await repo.UpdateAsync(entity, TestContext.Current.CancellationToken);

        Assert.True(updated);
        Assert.Equal("Updated", list[0].Name);
    }

    [Fact]
    public async Task Should_AddFieldKeyEntity_When_MutableList()
    {
        var list = new List<FieldKeyEntity>();
        var repo = list.AsRepository();
        var entity = new FieldKeyEntity { Name = "New" };

        await repo.AddAsync(entity, TestContext.Current.CancellationToken);

        Assert.Single(list);
        Assert.NotNull(list[0].Id);
    }

    [Fact]
    public void Should_FindByObjectKey_When_FieldKeyEntity()
    {
        var id = NewId();
        var entity = new FieldKeyEntity { Id = id, Name = "Test" };
        var list = new List<FieldKeyEntity> { entity };
        var repo = (IRepository<FieldKeyEntity, object>)list.AsRepository();

        var found = repo.FindAsync((object)id, TestContext.Current.CancellationToken).GetAwaiter().GetResult();

        Assert.NotNull(found);
        Assert.Equal(id, found.Id);
    }

    [Fact]
    public void Should_ThrowNotSupported_When_NoKeyAttribute()
    {
        var list = new List<NoKeyEntity>();
        var repo = list.AsRepository();
        var entity = new NoKeyEntity();

        Assert.Throws<NotSupportedException>(() => repo.GetEntityKey(entity));
    }

    [Fact]
    public void Should_ThrowNotSupported_When_MultipleKeyAttributes()
    {
        var list = new List<MultipleKeyEntity>();
        var repo = list.AsRepository();
        var entity = new MultipleKeyEntity();

        Assert.Throws<NotSupportedException>(() => repo.GetEntityKey(entity));
    }

    public class FieldKeyEntity
    {
        [Key]
        public string? Id;

        public string Name { get; set; } = string.Empty;
    }

    public class NoKeyEntity
    {
        public string? Name { get; set; }
    }

    public class MultipleKeyEntity
    {
        [Key]
        public string? Id1;

        [Key]
        public string? Id2;
    }
}
