using BenchmarkDotNet.Running;

using Kista.Benchmarks.Infrastructure;
using Kista.Benchmarks.Options;

BenchmarkRunPlan? runPlan = null;

try {
	var selection = DriverSelection.Parse(args);

	if (selection.ShowUsage) {
		DriverSelection.WriteUsage();
		return;
	}

	runPlan = BenchmarkConfigFactory.Create(selection);

	var summaries = BenchmarkSwitcher
		.FromTypes(selection.BenchmarkTypes)
		.Run(selection.BenchmarkArgs, runPlan.Config)
		.ToArray();

	BenchmarkResultFileExporter.WriteSingleOutputFileIfRequested(selection, runPlan, summaries);
} catch (ArgumentException ex) {
#pragma warning disable S6966 // Console.Error.WriteLine is appropriate in error handling paths for benchmark CLI
	Console.Error.WriteLine(ex.Message);
	Console.Error.WriteLine();
	DriverSelection.WriteUsage(Console.Error);
#pragma warning restore S6966
	Environment.ExitCode = 1;
} catch (InvalidOperationException ex) {
#pragma warning disable S6966
	Console.Error.WriteLine(ex.Message);
#pragma warning restore S6966
	Environment.ExitCode = 1;
} catch (FileNotFoundException ex) {
#pragma warning disable S6966
	Console.Error.WriteLine(ex.Message);
#pragma warning restore S6966
	Environment.ExitCode = 1;
} finally {
	BenchmarkResultFileExporter.CleanupTemporaryArtifacts(runPlan?.ArtifactsPath);
}

