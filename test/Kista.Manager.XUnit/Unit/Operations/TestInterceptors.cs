#pragma warning disable CS8618

namespace Kista;

/// <summary>
/// Shared interceptor test helpers for the operation-pipeline tests.
/// All helpers are <c>internal sealed</c> so they can be reused across
/// test classes without duplication (S3260, S4144).
/// </summary>
internal static class TestSystemTimeFactory {
	/// <summary>
	/// A fixed deterministic UTC timestamp used by <see cref="TestSystemTime"/>
	/// and asserted by the OnHooks tests.
	/// </summary>
	public static readonly DateTimeOffset FixedUtcNow =
		new(2026, 7, 12, 10, 0, 0, TimeSpan.Zero);
}

/// <summary>
/// An <see cref="ISystemTime"/> stub returning a fixed deterministic
/// timestamp, for tests that need to assert on the value written by
/// timestamp-stamping hooks.
/// </summary>
internal sealed class TestSystemTime : ISystemTime {
	public DateTimeOffset UtcNow => TestSystemTimeFactory.FixedUtcNow;
	public DateTimeOffset Now => UtcNow.ToLocalTime();
}

// --- Counting ---

internal sealed class CountingInterceptor : IEntityManagerInterceptor<Person, string> {
	public int PreWriteCount;
	public int PostWriteCount;

	public ValueTask<IOperationResult?> PreWriteAsync(IEntityOperationContext<Person, string> context) {
		PreWriteCount++;
		return default;
	}

	public ValueTask PostWriteAsync(IEntityOperationContext<Person, string> context, IOperationResult result) {
		PostWriteCount++;
		return ValueTask.CompletedTask;
	}
}

// --- Recording (call-order tracking, optional short-circuit) ---

internal sealed class RecordingInterceptor : IEntityManagerInterceptor<Person, string> {
	private readonly string _name;
	private readonly List<string> _callOrder;
	private readonly bool _shortCircuit;

	public RecordingInterceptor(string name, List<string> callOrder, bool shortCircuit = false) {
		_name = name;
		_callOrder = callOrder;
		_shortCircuit = shortCircuit;
	}

	public ValueTask<IOperationResult?> PreWriteAsync(IEntityOperationContext<Person, string> context) {
		_callOrder.Add($"{_name}.Pre");
		if (_shortCircuit)
			return new ValueTask<IOperationResult?>(OperationResult.Fail(new OperationError("SHORT_CIRCUIT", "Test", $"Short-circuited by {_name}")));
		return default;
	}

	public ValueTask PostWriteAsync(IEntityOperationContext<Person, string> context, IOperationResult result) {
		_callOrder.Add($"{_name}.Post");
		return ValueTask.CompletedTask;
	}
}

// --- Mutating ---

internal sealed class MutatingInterceptor : IEntityManagerInterceptor<Person, string> {
	private readonly Action<Person> _mutate;

	public MutatingInterceptor(Action<Person> mutate) {
		_mutate = mutate;
	}

	public ValueTask<IOperationResult?> PreWriteAsync(IEntityOperationContext<Person, string> context) {
		_mutate(context.Entity);
		return default;
	}

	public ValueTask PostWriteAsync(IEntityOperationContext<Person, string> context, IOperationResult result)
		=> ValueTask.CompletedTask;
}

// --- Short-circuit ---

internal sealed class ShortCircuitInterceptor : IEntityManagerInterceptor<Person, string> {
	public ValueTask<IOperationResult?> PreWriteAsync(IEntityOperationContext<Person, string> context)
		=> new(OperationResult.Fail(new OperationError("SHORT_CIRCUIT", "Test", "Short-circuited")));

	public ValueTask PostWriteAsync(IEntityOperationContext<Person, string> context, IOperationResult result)
		=> ValueTask.CompletedTask;
}

// --- Context capturing ---

internal sealed class ContextCapturingInterceptor : IEntityManagerInterceptor<Person, string> {
	public string? CapturedActor;
	public DateTimeOffset CapturedTimestamp;
	public EntityOperationKind CapturedKind;
	public Person? CapturedOriginal;
	public string? CapturedKey;

	public ValueTask<IOperationResult?> PreWriteAsync(IEntityOperationContext<Person, string> context) {
		CapturedActor = context.Actor;
		CapturedTimestamp = context.Timestamp;
		CapturedKind = context.Kind;
		CapturedOriginal = context.Original;
		CapturedKey = context.Key;
		return default;
	}

	public ValueTask PostWriteAsync(IEntityOperationContext<Person, string> context, IOperationResult result)
		=> ValueTask.CompletedTask;
}

// --- Result capturing ---

internal sealed class ResultCapturingInterceptor : IEntityManagerInterceptor<Person, string> {
	public IOperationResult? CapturedResult;

	public ValueTask<IOperationResult?> PreWriteAsync(IEntityOperationContext<Person, string> context)
		=> default;

	public ValueTask PostWriteAsync(IEntityOperationContext<Person, string> context, IOperationResult result) {
		CapturedResult = result;
		return ValueTask.CompletedTask;
	}
}

// --- Items bag: pre sets, post reads (single interceptor) ---

internal sealed class ItemsPrePostInterceptor : IEntityManagerInterceptor<Person, string> {
	public bool PostWriteSawPreWriteItem;

	public ValueTask<IOperationResult?> PreWriteAsync(IEntityOperationContext<Person, string> context) {
		context.Items["pre"] = "set";
		return default;
	}

	public ValueTask PostWriteAsync(IEntityOperationContext<Person, string> context, IOperationResult result) {
		PostWriteSawPreWriteItem = context.Items.TryGetValue("pre", out var val) && Equals(val, "set");
		return ValueTask.CompletedTask;
	}
}

// --- Items bag: split across two interceptors ---

internal sealed class ItemsSettingInterceptor : IEntityManagerInterceptor<Person, string> {
	private readonly string _key;
	private readonly object? _value;

	public ItemsSettingInterceptor(string key, object? value) {
		_key = key;
		_value = value;
	}

	public ValueTask<IOperationResult?> PreWriteAsync(IEntityOperationContext<Person, string> context) {
		context.Items[_key] = _value;
		return default;
	}

	public ValueTask PostWriteAsync(IEntityOperationContext<Person, string> context, IOperationResult result)
		=> ValueTask.CompletedTask;
}

internal sealed class ItemsReadingInterceptor : IEntityManagerInterceptor<Person, string> {
	private readonly string _key;
	private readonly object? _expectedValue;
	public bool ReadValueMatched;

	public ItemsReadingInterceptor(string key, object? expectedValue) {
		_key = key;
		_expectedValue = expectedValue;
	}

	public ValueTask<IOperationResult?> PreWriteAsync(IEntityOperationContext<Person, string> context) {
		ReadValueMatched = context.Items.TryGetValue(_key, out var val) && Equals(val, _expectedValue);
		return default;
	}

	public ValueTask PostWriteAsync(IEntityOperationContext<Person, string> context, IOperationResult result)
		=> ValueTask.CompletedTask;
}

// --- Custom short-circuit result (exercises ToOperationResult fallbacks) ---

internal sealed class CustomResultInterceptor : IEntityManagerInterceptor<Person, string> {
	private readonly IOperationResult _result;

	public CustomResultInterceptor(IOperationResult result) {
		_result = result;
	}

	public ValueTask<IOperationResult?> PreWriteAsync(IEntityOperationContext<Person, string> context)
		=> new(_result);

	public ValueTask PostWriteAsync(IEntityOperationContext<Person, string> context, IOperationResult result)
		=> ValueTask.CompletedTask;
}

/// <summary>
/// A custom <see cref="IOperationResult"/> implementation that is not an
/// <see cref="OperationResult"/>, to exercise the fallback branches of
/// <c>ToOperationResult</c>.
/// </summary>
internal sealed class CustomOperationResult : IOperationResult {
	private readonly bool _hasError;
	private readonly bool _isSuccess;

	public CustomOperationResult(bool hasError, bool isSuccess = false) {
		_hasError = hasError;
		_isSuccess = isSuccess;
	}

	public IOperationError? Error => _hasError ? new OperationError("CUSTOM", "Test", "Custom error") : null;
	public OperationResultType ResultType => _isSuccess ? OperationResultType.Success : OperationResultType.Error;
}

// --- Single-key interceptor (for EntityManager&lt;TEntity&gt;) ---

internal sealed class SingleKeyCountingInterceptor : IEntityManagerInterceptor<Person> {
	public int PreWriteCount;
	public int PostWriteCount;

	public ValueTask<IOperationResult?> PreWriteAsync(IEntityOperationContext<Person, object> context) {
		PreWriteCount++;
		return default;
	}

	public ValueTask PostWriteAsync(IEntityOperationContext<Person, object> context, IOperationResult result) {
		PostWriteCount++;
		return ValueTask.CompletedTask;
	}
}

// --- Generic variants (for SoftDeletablePerson and other entity types) ---

internal sealed class GenericCountingInterceptor<T, K> : IEntityManagerInterceptor<T, K>
	where T : class where K : notnull {
	public int PreWriteCount;
	public int PostWriteCount;

	public ValueTask<IOperationResult?> PreWriteAsync(IEntityOperationContext<T, K> context) {
		PreWriteCount++;
		return default;
	}

	public ValueTask PostWriteAsync(IEntityOperationContext<T, K> context, IOperationResult result) {
		PostWriteCount++;
		return ValueTask.CompletedTask;
	}
}

internal sealed class GenericRecordingInterceptor<T, K> : IEntityManagerInterceptor<T, K>
	where T : class where K : notnull {
	private readonly string _name;
	private readonly List<string> _callOrder;

	public GenericRecordingInterceptor(string name, List<string> callOrder) {
		_name = name;
		_callOrder = callOrder;
	}

	public ValueTask<IOperationResult?> PreWriteAsync(IEntityOperationContext<T, K> context) {
		_callOrder.Add($"{_name}.Pre");
		return default;
	}

	public ValueTask PostWriteAsync(IEntityOperationContext<T, K> context, IOperationResult result) {
		_callOrder.Add($"{_name}.Post");
		return ValueTask.CompletedTask;
	}
}

internal sealed class GenericShortCircuitInterceptor<T, K> : IEntityManagerInterceptor<T, K>
	where T : class where K : notnull {
	public ValueTask<IOperationResult?> PreWriteAsync(IEntityOperationContext<T, K> context)
		=> new(OperationResult.Fail(new OperationError("SHORT_CIRCUIT", "Test", "Short-circuited")));

	public ValueTask PostWriteAsync(IEntityOperationContext<T, K> context, IOperationResult result)
		=> ValueTask.CompletedTask;
}