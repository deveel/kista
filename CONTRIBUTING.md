# Contributing to Kista

We love your input! We want to make contributing to this project as easy and transparent as possible, whether it's:

- Reporting a bug
- Discussing the current state of the code
- Submitting a fix
- Proposing new features
- Becoming a maintainer

## We Develop with Github

We use GitHub to host code, to track issues and feature requests, as well as accept pull requests.

## GitHub Flow

We use the [GitHub Flow](https://guides.github.com/introduction/flow/index.html) — all code changes happen through pull requests.

Pull requests are the best way to propose changes to the codebase. We actively welcome your pull requests:

1. Fork the repo and create your branch from `main`.
2. If you've added code that should be tested, add tests.
3. If you changed APIs, update the documentation under `docs/`.
4. Ensure the test suite passes (`dotnet test`).
5. Make sure your code lints and builds without warnings.
6. Issue that pull request!

## Code Style and Conventions

The project enforces a baseline of style preferences through `.editorconfig`. Please keep your editor configured to honor it. The conventions below capture the decisions that are not (yet) enforced automatically.

### Indentation and formatting

- **Indentation**: tabs (width 4). `.editorconfig` sets `indent_style = tab`.
- **Line endings**: CRLF.
- **Braces**: preferred for multi-line bodies; single-statement bodies may omit braces (see `csharp_prefer_braces = when_multiline`).
- **Namespace declarations**: block-scoped (`namespace Kista { ... }`) for the historical codebase. New code should follow this style; do not mix file-scoped and block-scoped namespaces within the same project.
- **Using directives**: place outside the namespace, grouped with `System.*` first, then a blank line, then third-party namespaces.

### Namespaces

The codebase intentionally keeps most types in the root `namespace Kista`, regardless of the project folder. The `dotnet_style_namespace_match_folder` suggestion in `.editorconfig` is **not** applied here: do not rename namespaces to match folders unless the maintainer approves a migration. New driver packages may continue to use `namespace Kista` for their top-level types; subfolders (e.g. `Caching/`, `HealthChecks/Internal/`) use a matching sub-namespace.

### File naming for generic types

When a type has multiple generic arities, split them across separate files using the `_T` / `_T2` suffix convention:

| File | Declares |
|---|---|
| `Foo.cs` | The non-generic type `Foo` (or the base contract). |
| `Foo_T.cs` | The single-arity generic `Foo<TEntity>`. |
| `Foo_T2.cs` | The two-arity generic `Foo<TEntity, TKey>`. |

Non-generic siblings carry no suffix. Keep this convention when adding a new arity to an existing type family.

### Test projects

| Suffix | Purpose |
|---|---|
| `Kista.<Area>.XUnit` | Executable test project (`IsTestProject=true`). Class suffix is `Tests`. |
| `Kista.<Area>.Testing` | Shared support library for tests (`IsTestProject=false`). |

Test method naming follows `Should_<expected>_When_<condition>`.

### XML documentation

All public types and members must carry XML documentation comments (`<summary>`, `<param>`, `<returns>`, `<typeparam>`, `<exception>` as applicable). `<GenerateDocumentationFile>true</GenerateDocumentationFile>` is enabled, so missing docs surface as `CS1591` warnings and break the "build without warnings" goal. See the `xml-comments-doc` skill for detailed guidance.

### Copyright header

Every source file starts with the Apache 2.0 header below (update the year range as appropriate):

```csharp
// Copyright 2023-2026 Antonello Provenzano
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
```

## Project layout

```
src/        Libraries published to NuGet (one project per package).
test/       Test projects (.XUnit) and shared support libraries (.Testing).
samples/    Sample applications consuming the framework.
benchmarks/ Benchmark projects.
docs/       End-user documentation (rendered site).
```

Each `src/` project maps to a single NuGet package. Driver packages (e.g. `Kista.EntityFramework`, `Kista.MongoFramework`, `Kista.InMemory`) provide storage-specific implementations of the core abstractions in `Kista`.

## License of Your Contributions

When you submit code changes, your submissions are understood to be under the same [Apache 2.0 License](https://www.apache.org/licenses/LICENSE-2.0) that covers the project. Feel free to contact the maintainers if that's a concern.

## Reporting Bugs

We use GitHub [issues](https://github.com/deveel/kista/issues) to track public bugs. Report a bug by [opening a new issue](https://github.com/deveel/kista/issues/new); it's that easy!

### Write bug reports with detail, background, and sample code

**Great Bug Reports** tend to have:

- A quick summary and/or background
- Steps to reproduce
  - Be specific!
  - Give sample code if you can
- What you expected would happen
- What actually happens
- Notes (possibly including why you think this might be happening, or stuff you tried that didn't work)

People *love* thorough bug reports.