# Why Kista?
> **Renamed:** This project was renamed from **Deveel.Repository** to **Kista** on **May 26, 2025**. The name *Kista* is Old Norse for "chest" or "repository", better reflecting the project purpose as a data access framework.

## Motivation and Drivers

The [_Repository Pattern_](https://martinfowler.com/eaaCatalog/repository.html) is a well-established building block of [_Domain-Driven Design (DDD)_](https://en.wikipedia.org/wiki/Domain-driven_design). In DDD, a repository is not simply a data-access helper — it is the **boundary between the domain model and the infrastructure layer**. It speaks the language of the domain (entities, aggregates, identities) while hiding every detail of how data is fetched or persisted.

While building several projects, both internal and open-source, requiring some degree of data persistence, I found myself implementing the same pattern over and over again, with minor variations depending on the underlying data source. Existing libraries were either unreliable, too opinionated, or simply not providing the features I needed for multi-source scenarios.

This library was created to provide a simple, reliable, and framework-agnostic implementation of the pattern that can be used across projects without repeating the same boilerplate.

Although the Repository Pattern is not universally applicable (for example, in purely event-driven architectures), it remains one of the most practical tools for keeping domain logic clean and portable.

### This Is Not an ORM

> **It was never the intention of this project to build another ORM.**

Object-Relational Mappers (and document-mapper equivalents) such as [Entity Framework Core](https://github.com/dotnet/efcore), [Dapper](https://github.com/DapperLib/Dapper), or [MongoFramework](https://github.com/TurnerSoftware/MongoFramework) are excellent tools for mapping .NET objects to database tables or documents. They solve a real and complex problem at the infrastructure level.

Kista does not try to replace them. Instead, it provides a domain-oriented abstraction **on top of** them.

### How ORMs and Kista Coexist

| Concern | ORM (EF Core, Dapper, …) | Kista |
|---|---|---|
| **Responsibility** | Map objects ↔ storage (tables, documents) | Provide a domain-oriented access interface |
| **Speaks the language of** | Database schema, SQL, drivers | Domain model (entities, aggregates) |
| **Knows about** | Tables, columns, change tracking, joins | Collections of entities and their identities |
| **Lives in layer** | Infrastructure | Domain / Application boundary |
| **Used by** | Repository implementations | Application services, domain services |

In concrete terms: you build a repository _on top of_ an ORM. The ORM handles the low-level persistence mechanics; Kista defines the stable contract your application code depends on. When the database engine changes, or when you upgrade EF Core, only the repository implementation changes — your domain and application layers remain untouched.

The drivers shipped with this library (`Kista.EntityFramework`, `Kista.MongoFramework`) are precisely this: thin adapters that wrap an ORM behind the `IRepository<TEntity>` interface.

### Why Not Just Use Entity Framework Core Directly?

Entity Framework Core is a great tool, and this library actively uses it as one of its drivers. But EF Core is architecturally closer to an ORM than to a repository pattern:

- It exposes `DbSet<T>` and `IQueryable<T>` — infrastructure concerns — directly to consumers.
- It couples your application code to a specific persistence technology.
- It does not provide a uniform contract that allows you to swap storage backends (e.g., for testing or for supporting multiple databases).

By placing the `IRepository<TEntity>` abstraction between your domain and EF Core, you gain a stable, mockable, and swappable data-access layer — without giving up any of the power EF Core provides underneath.
