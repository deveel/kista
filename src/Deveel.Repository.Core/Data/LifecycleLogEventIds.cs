namespace Deveel.Data {
	static class LifecycleLogEventIds {
		public const int ResolvingHandler = 20000;
		public const int HandlerResolved = 20001;
		public const int FallingBack = 20002;
		public const int NoHandlerFound = 20003;
		public const int DeletingExisting = 20004;
		public const int SkippingExisting = 20005;
		public const int Creating = 20006;
		public const int Created = 20007;
		public const int NotExistsSkipping = 20008;
		public const int Dropping = 20009;
		public const int Dropped = 20010;
		public const int SkippingSeed = 20011;
		public const int Seeding = 20012;
		public const int SeedCompleted = 20013;
		public const int NoSeedData = 20014;
		public const int NotSupportedError = 20015;
		public const int RepositoryError = 20016;
		public const int GeneralError = 20017;
	}
}
