using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using Kista;

namespace Kista.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80, baseline: true)]
[SimpleJob(RuntimeMoniker.Net90)]
[SimpleJob(RuntimeMoniker.Net10_0)]
[HideColumns("Baseline", "RatioSD", "RatioSDMean", "RatioSDMedian")]
public class DynamicLinqFilterCacheBenchmarks {
	private static readonly string[] FilterExpressions = [
		"x.FirstName == \"John\"",
		"x.LastName == \"Smith\"",
		"x.Email != null && x.Email.StartsWith(\"admin\")",
		"x.FirstName.Contains(\"a\") && x.LastName.Contains(\"b\")",
		"x.FirstName == \"John\" || x.FirstName == \"Jane\"",
		"x.LastName.Length > 5",
		"x.Email != null && x.Email.EndsWith(\"@example.com\")",
		"x.FirstName.StartsWith(\"A\") && x.LastName.EndsWith(\"z\")"
	];

	private IFilterCache _cache = default!;

	[GlobalSetup]
	public void GlobalSetup() {
		_cache = new BoundedFilterCache(1024);
	}

	[Benchmark(Description = "ColdCache_ParseAndCompile")]
	public Delegate ColdCache_ParseAndCompile() {
		return FilterExpression.Compile(typeof(BenchPerson), "x", FilterExpressions[0]);
	}

	[Benchmark(Description = "WarmCache_CacheHit")]
	public Delegate WarmCache_CacheHit() {
		return FilterExpression.Compile(_cache, typeof(BenchPerson), "x", FilterExpressions[0]);
	}

	[Benchmark(Description = "WarmCache_MixedExpressions")]
	public Delegate[] WarmCache_MixedExpressions() {
		var results = new Delegate[FilterExpressions.Length];
		for (int i = 0; i < FilterExpressions.Length; i++) {
			results[i] = FilterExpression.Compile(_cache, typeof(BenchPerson), "x", FilterExpressions[i]);
		}
		return results;
	}

	[Benchmark(Description = "ColdCache_MultipleDistinctExpressions")]
	public Delegate[] ColdCache_MultipleDistinctExpressions() {
		var results = new Delegate[FilterExpressions.Length];
		for (int i = 0; i < FilterExpressions.Length; i++) {
			results[i] = FilterExpression.Compile(typeof(BenchPerson), "x", FilterExpressions[i]);
		}
		return results;
	}

	public sealed class BenchPerson {
		public string? Id { get; set; }
		public string FirstName { get; set; } = string.Empty;
		public string LastName { get; set; } = string.Empty;
		public string? Email { get; set; }
	}
}
