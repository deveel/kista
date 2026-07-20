using Xunit;

namespace Kista;

/// <summary>
/// Shared bodies for the two relationship-management tests
/// (<c>Should_AddRelationship_When_UpdatePersonWithNewRelationship</c> and
/// <c>Should_RemoveRelationship_When_UpdatePersonWithRelationshipRemoved</c>)
/// that are identical between
/// <see cref="NoKeyRepositoryTestSuite{TPerson, TRelationship}"/> (T2) and
/// <see cref="RepositoryTestSuite{TPerson, TKey, TRelationship}"/> (T3).
/// </summary>
/// <remarks>
/// The two suites cannot share a common base class for the relationship
/// tests because T3's relationship subclass must also inherit the TKey-typed
/// tests from <see cref="RepositoryTestSuite{TPerson, TKey}"/>, which already
/// occupies the single inheritance slot. Exposing the bodies as a static
/// helper invoked from both subclasses keeps the assertion logic in one place
/// without restructuring the suite hierarchy.
/// </remarks>
internal static class RelationshipTestCases {
	/// <summary>
	/// Asserts that updating a person with a newly-attached relationship
	/// persists the relationship through the repository.
	/// </summary>
	public static async Task AddRelationshipOnUpdateAsync<TPerson, TKey, TRelationship>(
		IReadOnlyList<TPerson> people,
		Func<TPerson, Task<TPerson?>> findAsync,
		Func<TPerson, TRelationship, Task> addRelationshipAsync,
		Func<TRelationship> generateRelationship,
		Func<TPerson, Task<bool>> updateAsync)
		where TPerson : class, IPerson<TKey>
		where TKey : notnull
		where TRelationship : class, IRelationship {
		// Arrange
		var person = people.Random(x => x.Relationships == null || !x.Relationships.Any());
		Assert.NotNull(person);

		var relationship = generateRelationship();
		var toUpdate = await findAsync(person);
		Assert.NotNull(toUpdate);

		await addRelationshipAsync(toUpdate!, relationship);

		// Act
		var result = await updateAsync(toUpdate!);

		// Assert
		Assert.True(result);

		var updated = await findAsync(person);
		Assert.NotNull(updated);
		Assert.NotNull(updated!.Relationships);
		Assert.NotEmpty(updated.Relationships);
		Assert.Single(updated.Relationships);
	}

	/// <summary>
	/// Asserts that updating a person with one of its relationships removed
	/// decreases the persisted relationship count by one.
	/// </summary>
	public static async Task RemoveRelationshipOnUpdateAsync<TPerson, TKey, TRelationship>(
		IReadOnlyList<TPerson> people,
		Func<TPerson, Task<TPerson?>> findAsync,
		Func<TPerson, TRelationship, Task> removeRelationshipAsync,
		Func<TPerson, Task<bool>> updateAsync)
		where TPerson : class, IPerson<TKey>
		where TKey : notnull
		where TRelationship : class, IRelationship {
		// Arrange
		var person = people.Random(x => x.Relationships?.Any() ?? false);
		Assert.NotNull(person);

		var toUpdate = await findAsync(person);
		Assert.NotNull(toUpdate);

		var relCount = toUpdate!.Relationships.Count();

		await removeRelationshipAsync(toUpdate, (TRelationship)toUpdate.Relationships!.First());

		// Act
		var result = await updateAsync(toUpdate);

		// Assert
		Assert.True(result);

		var updated = await findAsync(person);
		Assert.NotNull(updated);
		Assert.NotNull(updated!.Relationships);
		Assert.Equal(relCount - 1, updated.Relationships.Count());
	}
}