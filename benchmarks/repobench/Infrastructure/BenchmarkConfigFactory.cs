using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;

using Kista.Benchmarks.Options;

namespace Kista.Benchmarks.Infrastructure;

internal static class BenchmarkConfigFactory {
	private static readonly BenchmarkExportFormat[] DefaultExportFormats =
	[
		BenchmarkExportFormat.Plain,
		BenchmarkExportFormat.Markdown
	];

	public static BenchmarkRunPlan Create(DriverSelection selection) {
		var config = ManualConfig.CreateMinimumViable();
		var exportFormats = selection.ExportFormats.Count == 0
			? DefaultExportFormats
			: selection.ExportFormats.ToArray();
		string? artifactsPath = null;

		if (!String.IsNullOrWhiteSpace(selection.OutputPath)) {
			artifactsPath = Path.Combine(Path.GetTempPath(), "repobench", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(artifactsPath);
			config.ArtifactsPath = artifactsPath;
		}

		foreach (var exporter in CreateExporters(exportFormats)) {
			config.AddExporter(exporter);
		}

		return new BenchmarkRunPlan(config, exportFormats, artifactsPath);
	}

	private static IEnumerable<IExporter> CreateExporters(IReadOnlyCollection<BenchmarkExportFormat> exportFormats) {
		var requestedFormats = exportFormats.Count == 0
			? DefaultExportFormats
			: exportFormats.ToArray();

		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var exportFormat in requestedFormats) {
			var exporter = exportFormat switch {
				BenchmarkExportFormat.Markdown => MarkdownExporter.GitHub,
				BenchmarkExportFormat.Csv => CsvExporter.Default,
				BenchmarkExportFormat.Html => HtmlExporter.Default,
				BenchmarkExportFormat.Plain => PlainExporter.Default,
				_ => throw new ArgumentOutOfRangeException(nameof(exportFormat), exportFormat, "Unsupported export format")
			};

			if (seen.Add(exporter.Name))
				yield return exporter;
		}
	}
}




