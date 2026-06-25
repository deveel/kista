using System.ComponentModel.DataAnnotations;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Core")]
[Trait("Feature", "Repository")]
public class RepositoryWrapperTests
{
    private class KeyedEntity
    {
        [Key]
        public string? Id { get; set; }
        public string? Name { get; set; }
    }

    private class FieldKeyEntity
    {
        [Key]
        public string? Id;
        public string? Name { get; set; }
    }

    private class NoKeyEntity
    {
        public string? Name { get; set; }
    }

    private class MultipleKeyEntity
    {
        [Key]
        public string? Id1;
        [Key]
        public string? Id2;
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

    [Fact]
    public void Should_ThrowNotSupported_When_AddingToReadOnlyList()
    {
        var list = new List<KeyedEntity> { new() { Id = "1" } }.AsReadOnly();
        var repo = list.AsRepository();

        Assert.Throws<NotSupportedException>(() => repo.AddAsync(new KeyedEntity(), TestContext.Current.CancellationToken).GetAwaiter().GetResult());
    }

    [Fact]
    public void Should_ThrowNotSupported_When_RemovingFromReadOnlyList()
    {
        var list = new List<KeyedEntity> { new() { Id = "1" } }.AsReadOnly();
        var repo = list.AsRepository();

        Assert.Throws<NotSupportedException>(() => repo.RemoveAsync(new KeyedEntity { Id = "1" }, TestContext.Current.CancellationToken).GetAwaiter().GetResult());
    }

    [Fact]
    public void Should_ThrowNotSupported_When_UpdatingReadOnlyList()
    {
        var list = new List<KeyedEntity> { new() { Id = "1" } }.AsReadOnly();
        var repo = list.AsRepository();

        Assert.Throws<NotSupportedException>(() => repo.UpdateAsync(new KeyedEntity { Id = "1" }, TestContext.Current.CancellationToken).GetAwaiter().GetResult());
    }

    [Fact]
    public void Should_ReturnFalse_When_RemovingWithNullKey()
    {
        var list = new List<KeyedEntity> { new() { Id = "1" } };
        var repo = list.AsRepository();

        var result = repo.RemoveAsync(new KeyedEntity(), TestContext.Current.CancellationToken).GetAwaiter().GetResult();

        Assert.False(result);
    }

    [Fact]
    public void Should_ReturnFalse_When_UpdatingWithNullKey()
    {
        var list = new List<KeyedEntity> { new() { Id = "1" } };
        var repo = list.AsRepository();

        var result = repo.UpdateAsync(new KeyedEntity(), TestContext.Current.CancellationToken).GetAwaiter().GetResult();

        Assert.False(result);
    }

    [Fact]
    public void Should_ReturnFalse_When_UpdatingNonExistent()
    {
        var list = new List<KeyedEntity> { new() { Id = "1" } };
        var repo = list.AsRepository();

        var result = repo.UpdateAsync(new KeyedEntity { Id = "999" }, TestContext.Current.CancellationToken).GetAwaiter().GetResult();

        Assert.False(result);
    }

    [Fact]
    public void Should_ReturnFalse_When_RemovingNonExistent()
    {
        var list = new List<KeyedEntity> { new() { Id = "1" } };
        var repo = list.AsRepository();

        var result = repo.RemoveAsync(new KeyedEntity { Id = "999" }, TestContext.Current.CancellationToken).GetAwaiter().GetResult();

        Assert.False(result);
    }

    [Fact]
    public void Should_SkipNullKey_When_RemovingRange()
    {
        var list = new List<KeyedEntity> { new() { Id = "1" }, new() { Id = "2" } };
        var repo = list.AsRepository();

        repo.RemoveRangeAsync(new List<KeyedEntity> { new(), new() { Id = "1" } }, TestContext.Current.CancellationToken).GetAwaiter().GetResult();

        Assert.Single(list);
    }

    [Fact]
    public void Should_SkipNonExistent_When_RemovingRange()
    {
        var list = new List<KeyedEntity> { new() { Id = "1" } };
        var repo = list.AsRepository();

        repo.RemoveRangeAsync(new List<KeyedEntity> { new() { Id = "999" } }, TestContext.Current.CancellationToken).GetAwaiter().GetResult();

        Assert.Single(list);
    }

    [Fact]
    public void Should_FindFirst_WithFilter_OnEnumerable()
    {
        var list = new List<KeyedEntity> { new() { Id = "1", Name = "A" }, new() { Id = "2", Name = "B" } };
        var repo = list.AsRepository();
        var filterable = (IFilterableRepository<KeyedEntity>)repo;

        var result = filterable.FindFirstAsync(QueryFilter.Where<KeyedEntity>(x => x.Name == "B"), TestContext.Current.CancellationToken).GetAwaiter().GetResult();

        Assert.NotNull(result);
        Assert.Equal("B", result!.Name);
    }

    [Fact]
    public void Should_FindFirst_WithNoFilter_OnEnumerable()
    {
        var list = new List<KeyedEntity> { new() { Id = "1", Name = "A" }, new() { Id = "2", Name = "B" } };
        var repo = list.AsRepository();
        var filterable = (IFilterableRepository<KeyedEntity>)repo;

        var result = filterable.FindFirstAsync(QueryFilter.Empty, TestContext.Current.CancellationToken).GetAwaiter().GetResult();

        Assert.NotNull(result);
    }

    [Fact]
    public void Should_FindAll_WithFilter_OnEnumerable()
    {
        var list = new List<KeyedEntity> { new() { Id = "1", Name = "A" }, new() { Id = "2", Name = "B" }, new() { Id = "3", Name = "A" } };
        var repo = list.AsRepository();
        var filterable = (IFilterableRepository<KeyedEntity>)repo;

        var result = filterable.FindAllAsync(QueryFilter.Where<KeyedEntity>(x => x.Name == "A"), TestContext.Current.CancellationToken).GetAwaiter().GetResult();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Should_FindAll_WithNoFilter_OnEnumerable()
    {
        var list = new List<KeyedEntity> { new() { Id = "1" }, new() { Id = "2" } };
        var repo = list.AsRepository();
        var filterable = (IFilterableRepository<KeyedEntity>)repo;

        var result = filterable.FindAllAsync(QueryFilter.Empty, TestContext.Current.CancellationToken).GetAwaiter().GetResult();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Should_Exists_WithFilter_OnEnumerable()
    {
        var list = new List<KeyedEntity> { new() { Name = "A" } };
        var repo = list.AsRepository();
        var filterable = (IFilterableRepository<KeyedEntity>)repo;

        var result = filterable.ExistsAsync(QueryFilter.Where<KeyedEntity>(x => x.Name == "A"), TestContext.Current.CancellationToken).GetAwaiter().GetResult();

        Assert.True(result);
    }

    [Fact]
    public void Should_NotExists_WithFilter_OnEnumerable()
    {
        var list = new List<KeyedEntity> { new() { Name = "A" } };
        var repo = list.AsRepository();
        var filterable = (IFilterableRepository<KeyedEntity>)repo;

        var result = filterable.ExistsAsync(QueryFilter.Where<KeyedEntity>(x => x.Name == "Z"), TestContext.Current.CancellationToken).GetAwaiter().GetResult();

        Assert.False(result);
    }

    [Fact]
    public void Should_Count_WithFilter_OnEnumerable()
    {
        var list = new List<KeyedEntity> { new() { Name = "A" }, new() { Name = "A" }, new() { Name = "B" } };
        var repo = list.AsRepository();
        var filterable = (IFilterableRepository<KeyedEntity>)repo;

        var result = filterable.CountAsync(QueryFilter.Where<KeyedEntity>(x => x.Name == "A"), TestContext.Current.CancellationToken).GetAwaiter().GetResult();

        Assert.Equal(2, result);
    }

    [Fact]
    public void Should_GetPage_WithPageQuery()
    {
        var list = new List<KeyedEntity> { new() { Id = "1" }, new() { Id = "2" }, new() { Id = "3" } };
        var repo = list.AsRepository();

        var result = repo.GetPageAsync(new PageRequest(1, 2), TestContext.Current.CancellationToken).GetAwaiter().GetResult();

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(3, result.TotalItems);
    }

    [Fact]
    public void Should_GetPage_WithPageQueryFiltered()
    {
        var list = new List<KeyedEntity> { new() { Id = "1", Name = "A" }, new() { Id = "2", Name = "B" }, new() { Id = "3", Name = "A" } };
        var repo = list.AsRepository();

        var pageQuery = new PageQuery<KeyedEntity>(1, 2);
        pageQuery.Where(x => x.Name == "A");
        var result = repo.GetPageAsync(pageQuery, TestContext.Current.CancellationToken).GetAwaiter().GetResult();

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.TotalItems);
    }

    [Fact]
    public void Should_GetEntityKey_WithFieldKey()
    {
        var entity = new FieldKeyEntity { Id = "test-id" };
        var list = new List<FieldKeyEntity> { entity };
        var repo = list.AsRepository();

        var key = repo.GetEntityKey(entity);

        Assert.Equal("test-id", key);
    }

    [Fact]
    public void Should_AddFieldKeyEntity_When_MutableList()
    {
        var list = new List<FieldKeyEntity>();
        var repo = list.AsRepository();
        var entity = new FieldKeyEntity { Name = "New" };

        repo.AddAsync(entity, TestContext.Current.CancellationToken).GetAwaiter().GetResult();

        Assert.Single(list);
        Assert.NotNull(list[0].Id);
    }

    [Fact]
    public void Should_RemoveFieldKeyEntity_When_EntityExists()
    {
        var entity = new FieldKeyEntity { Id = "1", Name = "Test" };
        var list = new List<FieldKeyEntity> { entity };
        var repo = list.AsRepository();

        var removed = repo.RemoveAsync(entity, TestContext.Current.CancellationToken).GetAwaiter().GetResult();

        Assert.True(removed);
        Assert.Empty(list);
    }

    [Fact]
    public void Should_UpdateFieldKeyEntity_When_EntityExists()
    {
        var entity = new FieldKeyEntity { Id = "1", Name = "Original" };
        var list = new List<FieldKeyEntity> { entity };
        var repo = list.AsRepository();
        entity.Name = "Updated";

        var updated = repo.UpdateAsync(entity, TestContext.Current.CancellationToken).GetAwaiter().GetResult();

        Assert.True(updated);
        Assert.Equal("Updated", list[0].Name);
    }

    [Fact]
    public void Should_AddRange_WithFieldKeyEntities()
    {
        var list = new List<FieldKeyEntity>();
        var repo = list.AsRepository();

        repo.AddRangeAsync(new List<FieldKeyEntity> { new() { Name = "A" }, new() { Name = "B" } }, TestContext.Current.CancellationToken).GetAwaiter().GetResult();

        Assert.Equal(2, list.Count);
        Assert.NotNull(list[0].Id);
        Assert.NotNull(list[1].Id);
    }

    [Fact]
    public void Should_ThrowNotSupported_When_AddingToArray()
    {
        var array = new KeyedEntity[0];
        var repo = array.AsRepository();

        Assert.Throws<NotSupportedException>(() => repo.AddAsync(new KeyedEntity(), TestContext.Current.CancellationToken).GetAwaiter().GetResult());
    }

    [Fact]
    public void Should_ThrowNotSupported_When_RemovingFromArray()
    {
        var array = new[] { new KeyedEntity { Id = "1" } };
        var repo = array.AsRepository();

        Assert.Throws<NotSupportedException>(() => repo.RemoveAsync(array[0], TestContext.Current.CancellationToken).GetAwaiter().GetResult());
    }

    [Fact]
    public void Should_ThrowNotSupported_When_UpdatingArray()
    {
        var array = new[] { new KeyedEntity { Id = "1" } };
        var repo = array.AsRepository();

        Assert.Throws<NotSupportedException>(() => repo.UpdateAsync(array[0], TestContext.Current.CancellationToken).GetAwaiter().GetResult());
    }

    [Fact]
    public void Should_ThrowNotSupported_When_RemovingRangeFromArray()
    {
        var array = new[] { new KeyedEntity { Id = "1" } };
        var repo = array.AsRepository();

        Assert.Throws<NotSupportedException>(() => repo.RemoveRangeAsync(new[] { array[0] }, TestContext.Current.CancellationToken).GetAwaiter().GetResult());
    }

    [Fact]
    public void Should_UpdateWithList_When_UsingICollection()
    {
        var collection = new List<KeyedEntity> { new() { Id = "1", Name = "Old" } };
        var repo = collection.AsRepository();
        var updated = new KeyedEntity { Id = "1", Name = "New" };

        var result = repo.UpdateAsync(updated, TestContext.Current.CancellationToken).GetAwaiter().GetResult();

        Assert.True(result);
        Assert.Equal("New", collection[0].Name);
    }

    [Fact]
    public void Should_RemoveWithList_When_UsingICollection()
    {
        var collection = new List<KeyedEntity> { new() { Id = "1" } };
        var repo = collection.AsRepository();

        var result = repo.RemoveAsync(collection[0], TestContext.Current.CancellationToken).GetAwaiter().GetResult();

        Assert.True(result);
        Assert.Empty(collection);
    }
}
