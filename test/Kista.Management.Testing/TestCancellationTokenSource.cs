using Xunit;

namespace Kista;

public class TestCancellationTokenSource : IOperationCancellationSource {
    public CancellationToken Token => TestContext.Current.CancellationToken;
}
