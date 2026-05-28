namespace Kista;

public class TestUserAccessor<TKey> : IUserAccessor<TKey>
{
    private TKey? userId;

    public TKey? GetUserId() => userId;

    public void SetUserId(TKey? id) => userId = id;
}
