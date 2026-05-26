using BenchmarkDotNet.Configs;

using Kista.Benchmarks.Options;

namespace Kista.Benchmarks.Infrastructure;

internal sealed record BenchmarkRunPlan(
	IConfig Config,
	IReadOnlyList<BenchmarkExportFormat> ExportFormats,
	string? ArtifactsPath);

