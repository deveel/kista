# Kista.Benchmarks
> **Renamed:** This project was renamed from **Deveel.Repository** to **Kista** on **May 26, 2025**. The name *Kista* is Old Norse for "chest" or "repository", better reflecting the project purpose as a data access framework.

Unified BenchmarkDotNet runner for the repository implementations in this solution.

## Drivers

- `in-memory`
- `ef`
- `mongo`
- `all`

If no driver is specified, the runner defaults to `in-memory`.

## Exporters

- `markdown`
- `plain`
- `csv`
- `html`

You can repeat `--export`, pass a comma-separated list, or use `all`.

If no export is specified, the runner generates plain text and GitHub-flavored markdown reports.

> `ef` and `mongo` start disposable Docker containers through Testcontainers.

## Usage

```bash
dotnet run -c Release --framework net8.0 --project benchmarks/repobench/repobench.csproj -- --driver in-memory

dotnet run -c Release --framework net8.0 --project benchmarks/repobench/repobench.csproj -- --driver ef --filter '*FindAsync_ByKey*'

dotnet run -c Release --framework net8.0 --project benchmarks/repobench/repobench.csproj -- /driver:mongo --list flat

dotnet run -c Release --framework net8.0 --project benchmarks/repobench/repobench.csproj -- --driver all

dotnet run -c Release --framework net8.0 --project benchmarks/repobench/repobench.csproj -- --driver in-memory --output docs/benchmarks/in-memory.md

dotnet run -c Release --framework net8.0 --project benchmarks/repobench/repobench.csproj -- --driver ef --export csv --output docs/benchmarks/ef.csv
```

Any additional arguments are forwarded to BenchmarkDotNet.

The `--output` option now targets a single exported result file.

- If `--export` is omitted, the format is inferred from the output file extension (`.md`, `.csv`, `.html`, `.txt`).
- If `--output` is specified, exactly one export format must be selected.
- If `--output` is specified, `--driver all` is rejected because it would produce multiple benchmark reports.


