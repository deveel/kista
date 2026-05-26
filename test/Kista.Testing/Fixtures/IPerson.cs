namespace Kista {
	public interface IPerson<TKey> : IHaveTimeStamp where TKey : notnull {
		TKey? Id { get; set; }

		string FirstName { get; set; }

		string LastName { get; set; }

		string? Email { get; set; }

		DateTime? DateOfBirth { get; set; }

		string? PhoneNumber { get; set; }

		IEnumerable<IRelationship> Relationships { get; }
	}

	public interface IPerson : IPerson<string> {
	}
}
