namespace Kista {
	public readonly struct TestTime : ISystemTime {
		public TestTime() {
			var now = DateTimeOffset.UtcNow;
			UtcNow = now;
			Now = now.ToLocalTime();
		}

		public DateTimeOffset UtcNow { get; }

		public DateTimeOffset Now { get; }
	}
}
