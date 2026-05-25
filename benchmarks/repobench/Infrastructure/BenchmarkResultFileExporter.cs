using BenchmarkDotNet.Reports;

using Kista.Benchmarks.Options;

namespace Kista.Benchmarks.Infrastructure;

internal static class BenchmarkResultFileExporter {
	public static void WriteSingleOutputFileIfRequested(
		DriverSelection selection,
		BenchmarkRunPlan plan,
		IEnumerable<Summary> summaries) {
		if (String.IsNullOrWhiteSpace(selection.OutputPath))
			return;

		var summaryArray = summaries.ToArray();
		if (summaryArray.Length != 1)
			throw new InvalidOperationException("A single output file requires exactly one benchmark summary.");

		if (String.IsNullOrWhiteSpace(plan.ArtifactsPath))
			throw new InvalidOperationException("The benchmark artifacts path was not configured for single-file output.");

		var benchmarkName = selection.BenchmarkTypes.Single().Name;
		var sourceFile = ResolveExportedFile(plan.ArtifactsPath, benchmarkName, plan.ExportFormats.Single());
		var outputPath = Path.GetFullPath(selection.OutputPath);
		var outputDirectory = Path.GetDirectoryName(outputPath);

		if (!String.IsNullOrWhiteSpace(outputDirectory))
			Directory.CreateDirectory(outputDirectory);

		File.Copy(sourceFile, outputPath, overwrite: true);
	}

	public static void CleanupTemporaryArtifacts(string? artifactsPath) {
		if (String.IsNullOrWhiteSpace(artifactsPath))
			return;

		TryDeleteDirectory(artifactsPath);
	}

	private static string ResolveExportedFile(string artifactsPath, string benchmarkName, BenchmarkExportFormat exportFormat) {
		var fileName = exportFormat switch {
			BenchmarkExportFormat.Markdown => $"{benchmarkName}-report-github.md",
			BenchmarkExportFormat.Csv => $"{benchmarkName}-report.csv",
			BenchmarkExportFormat.Html => $"{benchmarkName}-report.html",
			BenchmarkExportFormat.Plain => $"{benchmarkName}-report.txt",
			_ => throw new ArgumentOutOfRangeException(nameof(exportFormat), exportFormat, "Unsupported export format")
		};

		var filePath = Path.Combine(artifactsPath, "results", fileName);
		if (!File.Exists(filePath))
			throw new FileNotFoundException($"The expected exported benchmark report '{fileName}' was not generated.", filePath);

		return filePath;
	}

	private static void TryDeleteDirectory(string path) {
		try {
			if (Directory.Exists(path))
				Directory.Delete(path, recursive: true);
		} catch {
			// Ignore cleanup failures: the exported output file has already been produced.
		}
	}
}


