namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Core")]
[Trait("Feature", "Infrastructure")]
public class DisposableHelperTests {
	[Fact]
	public void ThrowIfDisposed_NotDisposed_DoesNotThrow() {
		DisposableHelper.ThrowIfDisposed(false, typeof(DisposableHelperTests).FullName!);
	}

	[Fact]
	public void ThrowIfDisposed_Disposed_ThrowsObjectDisposed() {
		Assert.Throws<ObjectDisposedException>(
			() => DisposableHelper.ThrowIfDisposed(true, "TestType"));
	}
}
