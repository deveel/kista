namespace Kista;

public class TestCancellationTokenSource : IOperationCancellationSource
{
    public CancellationToken Token => CancellationToken.None;
}

