namespace Kista;

/// <summary>
/// A simple <see cref="IUserAccessor{TKey}"/> stub that returns a fixed
/// user identifier, for use in tests that need to simulate an
/// authenticated user context (e.g. soft-delete attribution).
/// </summary>
/// <typeparam name="TKey">
/// The type of the user identifier.
/// </typeparam>
public sealed class StaticUserAccessor<TKey> : IUserAccessor<TKey> {
	private readonly TKey? _userId;

	public StaticUserAccessor(TKey? userId) {
		_userId = userId;
	}

	public TKey? GetUserId() => _userId;
}