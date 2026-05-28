# Customize the Repository

In most of the circumstances, the default functions provided by the repository are enough, implementing the CRUD operations, but sometimes you need to add some custom functions to the repository. 

For example, you may want to add a function to get all the users that have a specific role, or you may want to add a function to get all the users that have a specific role and are active: in this case, you can use the query functions provided by implementations of the `IQueryableRepository` or the `IFilterableRepository` interface, when a driver supports them.

## Your Custom Repository

Depending on your desining style, you might want to extend the `IRepository<TEntity>` interface contract, or you might want to extend the specific drivers implementations, for example the `MongoRepository<TEntity>`.

If your intention is to being agnostic from the driver, you should extend the `IRepository<TEntity>` interface, for example:

```csharp
public interface IUserRepository : IRepository<User> {
    Task<IEnumerable<User>> GetUsersByRoleAsync(string role);
}
```

or even more generic:

```csharp
public interface IUserRepository<TUser> : IRepository<TUser> where TUser : class, IUser {
    Task<IEnumerable<User>> GetUsersByRoleAsync(string role);
}
```

## Registering the Custom Repository

Once you have defined your custom repository, you need to register it using the fluent builder API:

```csharp
// Program.cs
builder.Services.AddRepositoryContext()
    .UseInMemory()
    .AddRepository<UserRepository>();
```

The `AddRepository` method will register the repository as a scoped service by default, but you can still specify the lifetime you prefer.

The method is smart enough to scan the type specified for its base classes and interfaces that implement the `IRepository<TEntity>` interface, and register them as well.

In fact, once you have invoked the above method, you will have the following services registered:

| Service Type | Description |
| --- | --- |
| `UserRepository` | The concrete implementation of the custom repository you have defined |
| `IUserRepository` | The custom repository contract you have defined |
| `IRepository<User>` | The default repository for the `User` entity |


If your custom `IRepository<TEntity>` implements the `IQueryableRepository<TEntity>` or the `IFilterableRepository<TEntity>` interface, the `AddRepository` method will register the following services as well:

| Service Type | Description |
| --- | --- |
| `IQueryableRepository<User>` | A queryable repository for the `User` entity |
| `IFilterableRepository<User>` | A filterable repository for the `User` entity |