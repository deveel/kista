using System.ComponentModel.DataAnnotations;

namespace Kista;

public class CachingTestEntity {
	[Key]
	public string Id { get; set; } = Guid.NewGuid().ToString();
}

public class CachingTestRepository : IRepository<CachingTestEntity> {
	public ValueTask AddAsync(CachingTestEntity entity, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
	public ValueTask AddRangeAsync(IEnumerable<CachingTestEntity> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
	public ValueTask<bool> UpdateAsync(CachingTestEntity entity, CancellationToken cancellationToken = default) => new(false);
	public ValueTask<bool> RemoveAsync(CachingTestEntity entity, CancellationToken cancellationToken = default) => new(false);
	public ValueTask RemoveRangeAsync(IEnumerable<CachingTestEntity> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
	public ValueTask<CachingTestEntity?> FindAsync(object key, CancellationToken cancellationToken = default) => new((CachingTestEntity?)null);
	public object? GetEntityKey(CachingTestEntity entity) => (object?)entity.Id;
	public ValueTask<PageResult<CachingTestEntity>> GetPageAsync(PageRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}

public class SecondCachingEntity {
	[Key]
	public string Id { get; set; } = Guid.NewGuid().ToString();
}

public class SecondCachingRepository : IRepository<SecondCachingEntity> {
	public ValueTask AddAsync(SecondCachingEntity entity, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
	public ValueTask AddRangeAsync(IEnumerable<SecondCachingEntity> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
	public ValueTask<bool> UpdateAsync(SecondCachingEntity entity, CancellationToken cancellationToken = default) => new(false);
	public ValueTask<bool> RemoveAsync(SecondCachingEntity entity, CancellationToken cancellationToken = default) => new(false);
	public ValueTask RemoveRangeAsync(IEnumerable<SecondCachingEntity> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
	public ValueTask<SecondCachingEntity?> FindAsync(object key, CancellationToken cancellationToken = default) => new((SecondCachingEntity?)null);
	public object? GetEntityKey(SecondCachingEntity entity) => (object?)entity.Id;
	public ValueTask<PageResult<SecondCachingEntity>> GetPageAsync(PageRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}
