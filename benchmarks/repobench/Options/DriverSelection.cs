using Kista.Benchmarks;

namespace Kista.Benchmarks.Options;

internal sealed record DriverSelection(
	BenchmarkDriver Driver,
	string[] BenchmarkArgs,
	IReadOnlyList<BenchmarkExportFormat> ExportFormats,
	string? OutputPath,
	bool ShowUsage = false) {
	private static readonly Type[] InMemoryBenchmarks = [typeof(InMemoryRepositoryBenchmarks)];
	private static readonly Type[] EntityFrameworkBenchmarks = [typeof(EfRepositoryBenchmarks)];
	private static readonly Type[] MongoBenchmarks = [typeof(MongoRepositoryBenchmarks)];
	private static readonly Type[] DynamicLinqBenchmarks = [typeof(DynamicLinqFilterCacheBenchmarks)];
	private static readonly Type[] AllBenchmarks =
	[
		typeof(InMemoryRepositoryBenchmarks),
		typeof(EfRepositoryBenchmarks),
		typeof(MongoRepositoryBenchmarks),
		typeof(DynamicLinqFilterCacheBenchmarks)
	];

	public Type[] BenchmarkTypes => Driver switch {
		BenchmarkDriver.InMemory => InMemoryBenchmarks,
		BenchmarkDriver.EntityFramework => EntityFrameworkBenchmarks,
		BenchmarkDriver.Mongo => MongoBenchmarks,
		BenchmarkDriver.DynamicLinq => DynamicLinqBenchmarks,
		_ => AllBenchmarks
	};

	public static DriverSelection Parse(string[] args) {
		var benchmarkArgs = new List<string>(args.Length);
		var exportFormats = new List<BenchmarkExportFormat>();
		BenchmarkDriver driver = BenchmarkDriver.InMemory;
		string? outputPath = null;
		var showUsage = false;

		for (var i = 0; i < args.Length; i++) {
			var argument = args[i];

			if (TryReadOption(argument, "driver", out var driverValue, out var consumesNextArgument)) {
				driverValue = ReadOptionValue(ref i, args, driverValue, consumesNextArgument, "driver");

				if (driverValue.Equals("help", StringComparison.OrdinalIgnoreCase) ||
					driverValue.Equals("?", StringComparison.OrdinalIgnoreCase)) {
					showUsage = true;
					continue;
				}

				driver = ParseDriver(driverValue);
				continue;
			}

			if (TryReadOption(argument, "export", out var exportValue, out consumesNextArgument)) {
				exportValue = ReadOptionValue(ref i, args, exportValue, consumesNextArgument, "export");

				foreach (var exportFormat in ParseExportFormats(exportValue)) {
					exportFormats.Add(exportFormat);
				}

				continue;
			}

			if (TryReadOption(argument, "output", out var outputValue, out consumesNextArgument)) {
				outputValue = ReadOptionValue(ref i, args, outputValue, consumesNextArgument, "output");
				outputPath = outputValue;
				continue;
			}

			benchmarkArgs.Add(argument);
		}

		var normalizedExports = NormalizeExportFormats(exportFormats);

		ValidateOutput(outputPath, normalizedExports, ref driver);

		return new DriverSelection(driver, benchmarkArgs.ToArray(), normalizedExports, outputPath, showUsage);
	}

	private static string ReadOptionValue(ref int i, string[] args, string? value, bool consumesNextArgument, string optionName) {
		if (!string.IsNullOrWhiteSpace(value))
			return value;

		if (!consumesNextArgument || i + 1 >= args.Length)
			throw new ArgumentException($"The {optionName} option requires a value.");

		return args[++i];
	}

	private static void ValidateOutput(string? outputPath, List<BenchmarkExportFormat> normalizedExports, ref BenchmarkDriver driver) {
		if (string.IsNullOrWhiteSpace(outputPath))
			return;

		if (normalizedExports.Count == 0)
			normalizedExports.Add(InferExportFormatFromFileName(outputPath));

		if (normalizedExports.Count != 1)
			throw new ArgumentException("When output is specified, exactly one export format must be selected.");

		if (driver == BenchmarkDriver.All)
			throw new ArgumentException("When output is specified, select a single driver instead of 'all'.");
	}

	public static void WriteUsage(TextWriter? writer = null) {
		writer ??= Console.Out;
		writer.WriteLine("Unified repository benchmarks");
		writer.WriteLine();
		writer.WriteLine("Driver options:");
		writer.WriteLine("  --driver in-memory");
		writer.WriteLine("  --driver ef");
		writer.WriteLine("  --driver mongo");
		writer.WriteLine("  --driver dynamic-linq");
		writer.WriteLine("  --driver all");
		writer.WriteLine("  /driver:in-memory");
		writer.WriteLine("  --export markdown,csv,html,plain");
		writer.WriteLine("  --output docs/benchmarks/in-memory.md");
		writer.WriteLine();
		writer.WriteLine("If no driver is specified, the default is 'in-memory'.");
		writer.WriteLine("If no export is specified, the runner generates plain text and GitHub-flavored markdown reports.");
		writer.WriteLine("When output is specified, it must be a single result file path and only one export format is allowed.");
		writer.WriteLine("Entity Framework and Mongo benchmarks require Docker because they bootstrap containers via Testcontainers.");
		writer.WriteLine();
		writer.WriteLine("Examples:");
		writer.WriteLine("  dotnet run -c Release --framework net8.0 --project benchmarks/repobench/repobench.csproj -- --driver in-memory");
		writer.WriteLine("  dotnet run -c Release --framework net8.0 --project benchmarks/repobench/repobench.csproj -- --driver ef --filter '*FindAsync_ByKey*'");
		writer.WriteLine("  dotnet run -c Release --framework net8.0 --project benchmarks/repobench/repobench.csproj -- /driver:mongo --list flat");
		writer.WriteLine("  dotnet run -c Release --framework net8.0 --project benchmarks/repobench/repobench.csproj -- --driver in-memory --output docs/benchmarks/in-memory.md");
		writer.WriteLine("  dotnet run -c Release --framework net8.0 --project benchmarks/repobench/repobench.csproj -- --driver dynamic-linq");
	}

	private static List<BenchmarkExportFormat> NormalizeExportFormats(IEnumerable<BenchmarkExportFormat> exportFormats) {
		var normalized = new List<BenchmarkExportFormat>();
		var seen = new HashSet<BenchmarkExportFormat>();

		foreach (var exportFormat in exportFormats) {
			if (seen.Add(exportFormat))
				normalized.Add(exportFormat);
		}

		return normalized;
	}

	private static BenchmarkExportFormat InferExportFormatFromFileName(string outputPath) {
		var extension = Path.GetExtension(outputPath);

		return extension.ToLowerInvariant() switch {
			".md" or ".markdown" => BenchmarkExportFormat.Markdown,
			".csv" => BenchmarkExportFormat.Csv,
			".html" or ".htm" => BenchmarkExportFormat.Html,
			".txt" => BenchmarkExportFormat.Plain,
			_ => throw new ArgumentException("When output is specified without --export, the file extension must be one of: .md, .csv, .html, .txt.")
		};
	}

	private static bool TryReadOption(string argument, string optionName, out string? value, out bool consumesNextArgument) {
		value = null;
		consumesNextArgument = false;

		if (argument.Equals($"--{optionName}", StringComparison.OrdinalIgnoreCase) ||
			argument.Equals($"/{optionName}", StringComparison.OrdinalIgnoreCase)) {
			consumesNextArgument = true;
			return true;
		}

		const StringComparison comparison = StringComparison.OrdinalIgnoreCase;
		var prefixes = new[] { $"--{optionName}=", $"--{optionName}:", $"/{optionName}=", $"/{optionName}:" };
		foreach (var prefix in prefixes) {
			if (argument.StartsWith(prefix, comparison)) {
				value = argument[prefix.Length..];
				return true;
			}
		}

		return false;
	}

	private static IEnumerable<BenchmarkExportFormat> ParseExportFormats(string value) {
		foreach (var token in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
			var normalized = token.Trim().ToLowerInvariant();

			if (normalized is "all" or "*") {
				yield return BenchmarkExportFormat.Markdown;
				yield return BenchmarkExportFormat.Csv;
				yield return BenchmarkExportFormat.Html;
				yield return BenchmarkExportFormat.Plain;
				continue;
			}

			yield return normalized switch {
				"md" or "markdown" or "github-markdown" => BenchmarkExportFormat.Markdown,
				"csv" => BenchmarkExportFormat.Csv,
				"html" => BenchmarkExportFormat.Html,
				"plain" or "text" or "txt" => BenchmarkExportFormat.Plain,
				_ => throw new ArgumentException($"Unsupported export format '{token}'.")
			};
		}
	}

	private static BenchmarkDriver ParseDriver(string value) {
		return value.Trim().ToLowerInvariant() switch {
			"in-memory" or "inmemory" or "memory" => BenchmarkDriver.InMemory,
			"ef" or "entityframework" or "entity-framework" => BenchmarkDriver.EntityFramework,
			"mongo" or "mongodb" => BenchmarkDriver.Mongo,
			"dynamiclinq" or "dynamic-linq" or "dlinq" => BenchmarkDriver.DynamicLinq,
			"all" => BenchmarkDriver.All,
			_ => throw new ArgumentException($"Unsupported benchmark driver '{value}'.")
		};
	}
}

