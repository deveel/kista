namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "BoundedFilterCache")]
public class BoundedFilterCacheTests {
	[Fact]
	public void Constructor_WithValidCapacity_CreatesCache() {
		var cache = new BoundedFilterCache(100);

		Assert.NotNull(cache.Statistics);
		Assert.Equal(100, cache.Statistics.MaxCapacity);
		Assert.Equal(0, cache.Statistics.CurrentSize);
	}

	[Fact]
	public void Constructor_WithDefaultOptions_UsesDefaultCapacity() {
		var cache = new BoundedFilterCache();

		Assert.Equal(1024, cache.Statistics.MaxCapacity);
	}

	[Fact]
	public void Constructor_WithOptions_AppliesOptions() {
		var options = new BoundedFilterCacheOptions { MaxCapacity = 50 };
		var cache = new BoundedFilterCache(options);

		Assert.Equal(50, cache.Statistics.MaxCapacity);
	}

	[Fact]
	public void Constructor_WithCapacityLessThanOne_Throws() {
		Assert.Throws<ArgumentOutOfRangeException>(() => new BoundedFilterCache(0));
		Assert.Throws<ArgumentOutOfRangeException>(() => new BoundedFilterCache(-1));
	}

	[Fact]
	public void Constructor_WithNullOptions_Throws() {
		Assert.Throws<ArgumentNullException>(() => new BoundedFilterCache(null!));
	}

	[Fact]
	public void Set_And_TryGet_RetrievesCachedDelegate() {
		var cache = new BoundedFilterCache(10);
		Delegate func = (Func<Person, bool>)(p => p.FirstName == "John");

		cache.Set("test", func);

		Assert.True(cache.TryGet("test", out var retrieved));
		Assert.Same(func, retrieved);
	}

	[Fact]
	public void TryGet_MissingKey_ReturnsFalse() {
		var cache = new BoundedFilterCache(10);

		Assert.False(cache.TryGet("nonexistent", out var result));
		Assert.Null(result);
	}

	[Fact]
	public void TryGet_NullExpression_Throws() {
		var cache = new BoundedFilterCache(10);

		Assert.Throws<ArgumentNullException>(() => cache.TryGet(null!, out _));
	}

	[Fact]
	public void Set_NullExpression_Throws() {
		var cache = new BoundedFilterCache(10);
		Delegate func = (Func<Person, bool>)(p => true);

		Assert.Throws<ArgumentNullException>(() => cache.Set(null!, func));
	}

	[Fact]
	public void Set_NullDelegate_Throws() {
		var cache = new BoundedFilterCache(10);

		Assert.Throws<ArgumentNullException>(() => cache.Set("test", null!));
	}

	[Fact]
	public void Statistics_TracksHitsAndMisses() {
		var cache = new BoundedFilterCache(10);
		Delegate func = (Func<Person, bool>)(p => true);
		cache.Set("expr1", func);

		cache.TryGet("expr1", out _);
		cache.TryGet("expr1", out _);
		cache.TryGet("missing", out _);

		Assert.Equal(2, cache.Statistics.Hits);
		Assert.Equal(1, cache.Statistics.Misses);
	}

	[Fact]
	public void Statistics_HitRate_ReturnsCorrectRatio() {
		var cache = new BoundedFilterCache(10);
		Delegate func = (Func<Person, bool>)(p => true);
		cache.Set("expr1", func);

		cache.TryGet("expr1", out _);
		cache.TryGet("expr1", out _);
		cache.TryGet("missing", out _);
		cache.TryGet("missing2", out _);

		Assert.Equal(0.5, cache.Statistics.HitRate);
	}

	[Fact]
	public void Statistics_HitRate_ReturnsZero_WhenNoLookups() {
		var cache = new BoundedFilterCache(10);

		Assert.Equal(0, cache.Statistics.HitRate);
	}

	[Fact]
	public void Statistics_Reset_ClearsCounters() {
		var cache = new BoundedFilterCache(10);
		Delegate func = (Func<Person, bool>)(p => true);
		cache.Set("expr1", func);

		cache.TryGet("expr1", out _);
		cache.TryGet("missing", out _);
		cache.Statistics.Reset();

		Assert.Equal(0, cache.Statistics.Hits);
		Assert.Equal(0, cache.Statistics.Misses);
		Assert.Equal(1, cache.Statistics.CurrentSize);
	}

	[Fact]
	public void LruEviction_RemovesLeastRecentlyUsed_WhenCapacityExceeded() {
		var cache = new BoundedFilterCache(3);
		Delegate func1 = (Func<Person, bool>)(p => p.FirstName == "A");
		Delegate func2 = (Func<Person, bool>)(p => p.FirstName == "B");
		Delegate func3 = (Func<Person, bool>)(p => p.FirstName == "C");
		Delegate func4 = (Func<Person, bool>)(p => p.FirstName == "D");

		cache.Set("expr1", func1);
		cache.Set("expr2", func2);
		cache.Set("expr3", func3);

		cache.Set("expr4", func4);

		Assert.False(cache.TryGet("expr1", out _));
		Assert.True(cache.TryGet("expr2", out _));
		Assert.True(cache.TryGet("expr3", out _));
		Assert.True(cache.TryGet("expr4", out _));
	}

	[Fact]
	public void LruEviction_AccessedItemsAreNotEvicted() {
		var cache = new BoundedFilterCache(3);
		Delegate func1 = (Func<Person, bool>)(p => p.FirstName == "A");
		Delegate func2 = (Func<Person, bool>)(p => p.FirstName == "B");
		Delegate func3 = (Func<Person, bool>)(p => p.FirstName == "C");
		Delegate func4 = (Func<Person, bool>)(p => p.FirstName == "D");

		cache.Set("expr1", func1);
		cache.Set("expr2", func2);
		cache.Set("expr3", func3);

		cache.TryGet("expr1", out _);

		cache.Set("expr4", func4);

		Assert.True(cache.TryGet("expr1", out _));
		Assert.False(cache.TryGet("expr2", out _));
		Assert.True(cache.TryGet("expr3", out _));
		Assert.True(cache.TryGet("expr4", out _));
	}

	[Fact]
	public void LruEviction_CurrentSizeNeverExceedsCapacity() {
		var cache = new BoundedFilterCache(5);

		for (int i = 0; i < 20; i++) {
			Delegate func = (Func<Person, bool>)(p => true);
			cache.Set($"expr{i}", func);
		}

		Assert.Equal(5, cache.Statistics.CurrentSize);
	}

	[Fact]
	public void Set_ExistingKey_UpdatesValueAndRefreshesOrder() {
		var cache = new BoundedFilterCache(3);
		Delegate func1 = (Func<Person, bool>)(p => p.FirstName == "A");
		Delegate func2 = (Func<Person, bool>)(p => p.FirstName == "B");
		Delegate func3 = (Func<Person, bool>)(p => p.FirstName == "C");
		Delegate func4 = (Func<Person, bool>)(p => p.FirstName == "D");
		Delegate func1Updated = (Func<Person, bool>)(p => p.FirstName == "A-Updated");

		cache.Set("expr1", func1);
		cache.Set("expr2", func2);
		cache.Set("expr3", func3);

		cache.Set("expr1", func1Updated);

		cache.Set("expr4", func4);

		Assert.True(cache.TryGet("expr1", out var retrieved));
		Assert.Same(func1Updated, retrieved);
		Assert.False(cache.TryGet("expr2", out _));
	}

	[Fact]
	public void Clear_RemovesAllEntries() {
		var cache = new BoundedFilterCache(10);
		Delegate func = (Func<Person, bool>)(p => true);

		cache.Set("expr1", func);
		cache.Set("expr2", func);
		cache.Set("expr3", func);

		cache.Clear();

		Assert.Equal(0, cache.Statistics.CurrentSize);
		Assert.False(cache.TryGet("expr1", out _));
		Assert.False(cache.TryGet("expr2", out _));
	}

	[Fact]
	public void Clear_DoesNotResetStatistics() {
		var cache = new BoundedFilterCache(10);
		Delegate func = (Func<Person, bool>)(p => true);

		cache.Set("expr1", func);
		cache.TryGet("expr1", out _);
		cache.TryGet("missing", out _);

		cache.Clear();

		Assert.Equal(1, cache.Statistics.Hits);
		Assert.Equal(1, cache.Statistics.Misses);
	}

	[Fact]
	public void CacheKey_IsCaseSensitive() {
		var cache = new BoundedFilterCache(10);
		Delegate func1 = (Func<Person, bool>)(p => true);
		Delegate func2 = (Func<Person, bool>)(p => false);

		cache.Set("Expression", func1);
		cache.Set("expression", func2);

		Assert.True(cache.TryGet("Expression", out var retrieved1));
		Assert.True(cache.TryGet("expression", out var retrieved2));
		Assert.Same(func1, retrieved1);
		Assert.Same(func2, retrieved2);
	}

	[Fact]
	public void Cache_IsThreadSafe() {
		var cache = new BoundedFilterCache(100);
		var exceptions = new List<Exception>();
		var threads = new List<Thread>();

		for (int t = 0; t < 10; t++) {
			var threadIndex = t;
			var thread = new Thread(() => {
				try {
					for (int i = 0; i < 100; i++) {
						var key = $"expr-{threadIndex}-{i}";
						Delegate func = (Func<Person, bool>)(p => true);
						cache.Set(key, func);
						cache.TryGet(key, out _);
					}
				} catch (Exception ex) {
					lock (exceptions) {
						exceptions.Add(ex);
					}
				}
			});
			threads.Add(thread);
		}

		foreach (var thread in threads) {
			thread.Start();
		}

		foreach (var thread in threads) {
			thread.Join();
		}

		Assert.Empty(exceptions);
		Assert.True(cache.Statistics.CurrentSize <= 100);
	}
}
