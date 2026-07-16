namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
[Trait("Feature", "OperationPipeline")]
public class EntityOperationContextTests {
	[Fact]
	public void Should_ConstructContextWithAllProperties() {
		var entity = new Person { Id = "1", FirstName = "Test" };
		var original = new Person { Id = "1", FirstName = "Original" };
		var timestamp = new DateTimeOffset(2026, 7, 12, 10, 0, 0, TimeSpan.Zero);
		var cts = new CancellationTokenSource();
		var token = cts.Token;

		var context = new EntityOperationContext<Person, string>(
			EntityOperationKind.Update,
			entity,
			original,
			"1",
			"user-42",
			timestamp,
			token);

		Assert.Equal(EntityOperationKind.Update, context.Kind);
		Assert.Same(entity, context.Entity);
		Assert.Same(original, context.Original);
		Assert.Equal("1", context.Key);
		Assert.Equal("user-42", context.Actor);
		Assert.Equal(timestamp, context.Timestamp);
		Assert.Equal(token, context.CancellationToken);
	}

	[Fact]
	public void Should_AllowNullOriginalForCreate() {
		var entity = new Person { Id = "1" };

		var context = new EntityOperationContext<Person, string>(
			EntityOperationKind.Create,
			entity,
			null,
			"1",
			null,
			default,
			default);

		Assert.Equal(EntityOperationKind.Create, context.Kind);
		Assert.Null(context.Original);
		Assert.Null(context.Actor);
	}

	[Fact]
	public void Should_AllowMutableEntity() {
		var entity = new Person { Id = "1", FirstName = "Before" };
		var context = new EntityOperationContext<Person, string>(
			EntityOperationKind.Create,
			entity,
			null,
			"1",
			null,
			default,
			default);

		context.Entity.FirstName = "After";

		Assert.Equal("After", context.Entity.FirstName);
		Assert.Equal("After", entity.FirstName);
	}

	[Fact]
	public void Should_ShareItemsBagAcrossAccesses() {
		var entity = new Person { Id = "1" };
		var context = new EntityOperationContext<Person, string>(
			EntityOperationKind.Create,
			entity,
			null,
			"1",
			null,
			default,
			default);

		context.Items["foo"] = 42;

		Assert.True(context.Items.ContainsKey("foo"));
		Assert.Equal(42, context.Items["foo"]);
	}

	[Fact]
	public void Should_InitializeEmptyItemsBag() {
		var entity = new Person { Id = "1" };
		var context = new EntityOperationContext<Person, string>(
			EntityOperationKind.Create,
			entity,
			null,
			"1",
			null,
			default,
			default);

		Assert.NotNull(context.Items);
		Assert.Empty(context.Items);
	}
}