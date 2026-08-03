#pragma warning disable CS8618

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Core")]
[Trait("Feature", "RepositoryWrapper")]
public class RepositoryWrapperBranchTests : IClassFixture<PersonFixture> {
	private readonly PersonFixture _fixture;

	public RepositoryWrapperBranchTests(PersonFixture fixture) {
		_fixture = fixture;
	}

	[Fact]
	public async Task Should_ReturnFilteredPage_When_GetPageAsyncWithPageQuery() {
		var people = _fixture.BuildPeople(50);
		var repo = people.AsRepository();
		var ct = TestContext.Current.CancellationToken;

		var query = new PageQuery<Person>(1, 10)
			.Where(p => p.FirstName.Length > 0)
			.OrderBy(p => p.FirstName);

		var page = await repo.GetPageAsync(query, ct);

		Assert.Equal(50, page.TotalItems);
		Assert.Equal(10, page.Items.Count);
	}

	[Fact]
	public async Task Should_RemoveViaICollection_When_NotIList() {
		var people = new System.Collections.ObjectModel.Collection<Person>(_fixture.BuildPeople(10).ToList());
		var repo = people.AsRepository();
		var ct = TestContext.Current.CancellationToken;

		var target = people[0];
		var result = await repo.RemoveAsync(target, ct);

		Assert.True(result);
		Assert.DoesNotContain(target, people);
	}

	[Fact]
	public async Task Should_UpdateViaICollection_When_NotIList() {
		var people = new System.Collections.ObjectModel.Collection<Person>(_fixture.BuildPeople(10).ToList());
		var repo = people.AsRepository();
		var ct = TestContext.Current.CancellationToken;

		var target = people[0];
		target.FirstName = "Updated-Name";
		var result = await repo.UpdateAsync(target, ct);

		Assert.True(result);
		Assert.Contains(people, p => p.FirstName == "Updated-Name");
	}

	[Fact]
	public async Task Should_ThrowNotSupported_When_RemoveOnNonICollectionEnumerable() {
		var people = _fixture.BuildPeople(10).ToList();
		IEnumerable<Person> nonCollection = people.Where(p => p != null);
		var repo = nonCollection.AsRepository();
		var ct = TestContext.Current.CancellationToken;

		await Assert.ThrowsAsync<NotSupportedException>(() =>
			repo.RemoveAsync(people[0], ct).AsTask());
	}

	[Fact]
	public async Task Should_ThrowNotSupported_When_UpdateOnNonICollectionEnumerable() {
		var people = _fixture.BuildPeople(10).ToList();
		IEnumerable<Person> nonCollection = people.Where(p => p != null);
		var repo = nonCollection.AsRepository();
		var ct = TestContext.Current.CancellationToken;

		await Assert.ThrowsAsync<NotSupportedException>(() =>
			repo.UpdateAsync(people[0], ct).AsTask());
	}
}