# Dynamic LINQ Security

`Kista.DynamicLinq` bridges string-based filter expressions with the strongly-typed
LINQ expression-tree API used by repository drivers. When the expression string
originates from an untrusted source (e.g. an API client), Dynamic LINQ is a potential
remote-code-execution (RCE) surface.

## Hardened Parsing Configuration

Kista parses every expression string with [`KistaParsingConfig`][kpc], a hardened
[`ParsingConfig`][pc] that closes the most dangerous vectors:

| Option | Value | Blocks |
|---|---|---|
| `DisallowNewKeyword` | `true` | The `new` operator — prevents `new System.Diagnostics.Process()` and similar instantiations of arbitrary types. |
| `SupportCastingToFullyQualifiedTypeAsString` | `false` | Fully-qualified type casts — prevents `(System.Diagnostics.Process) null`. |

Static method calls to arbitrary types (e.g. `System.IO.File.Exists("x")`) are blocked
by the default `IDynamicLinqCustomTypeProvider`, which restricts type resolution to the
`System` namespace enums and a small set of primitives.

## Defense-in-Depth, Not a Sandbox

`KistaParsingConfig` is **defense-in-depth**, not a complete sandbox. It blocks the
known RCE vectors, but:

- It does not restrict which **properties** of the entity can be referenced — an
  attacker can still read any public property of `TEntity` in the filter.
- It does not restrict **operators** — an attacker can craft computationally expensive
  expressions (e.g. deeply nested boolean trees) to consume CPU.
- Future versions of `System.Linq.Dynamic.Core` may introduce new vectors that
  `KistaParsingConfig` does not cover.

## Application-Level Allow-List

If your expression strings come from untrusted input, you **must** additionally apply
an application-level allow-list before constructing a `DynamicLinqFilter`:

1. **Restrict fields** — parse the expression with your own grammar and reject any
   field name that is not in an explicit allow-list for the entity type.
2. **Restrict operators** — reject expressions that contain operators outside the
   permitted set (`==`, `!=`, `>`, `<`, `>=`, `<=`, `&&`, `||`, `Contains`, `StartsWith`).
3. **Bound length** — reject expressions longer than a reasonable limit (e.g. 1024
   characters) to prevent CPU-exhaustion attacks.
4. **Never** pass raw client input as the expression string without validation.

## Example: Safe Usage

```csharp
// GOOD: expression string is built by the application from validated parameters
var filter = new DynamicLinqFilter($"x.Status == \"{status}\"");

// GOOD: expression string comes from a trusted configuration source
var filter = new DynamicLinqFilter(config.FilterExpression);

// BAD: expression string comes directly from an API client without validation
var filter = new DynamicLinqFilter(request.Body.FilterExpression); // RCE/DoS risk
```

## See Also

- [Filter Expressions](filter-cache.md)
- [System.Linq.Dynamic.Core Security](https://github.com/zzzprojects/System.Linq.Dynamic.Core)

[kpc]: ../../api/Kista.DynamicLinq.KistaParsingConfig.yml
[pc]: https://github.com/zzzprojects/System.Linq.Dynamic.Core