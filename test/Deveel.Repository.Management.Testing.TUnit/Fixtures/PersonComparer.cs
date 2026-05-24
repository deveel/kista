using System.Diagnostics.CodeAnalysis;

namespace Deveel.Data {
	public class PersonComparer<TPerson, TKey> : IEqualityComparer<TPerson>
		where TPerson : class, IPerson<TKey>
		where TKey : notnull {

		public bool Equals(TPerson? person, TPerson? other) {
			if (person == null && other == null)
				return true;
			if (person == null || other == null)
				return false;

			if (!Equals(person.Id, other.Id))
				return false;

			if (person.FirstName != other.FirstName ||
				person.LastName != other.LastName ||
				person.Email != other.Email ||
				person.PhoneNumber != other.PhoneNumber ||
				person.DateOfBirth != other.DateOfBirth)
				return false;


			// Related entities are unreliable to compare
			// because they are not loaded in the same way

			return true;
		}

		public int GetHashCode([DisallowNull] TPerson obj) => throw new NotImplementedException();
	}
}
