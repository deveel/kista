using System.Diagnostics.CodeAnalysis;

namespace Kista {
	public class PersonComparer<TPerson, TKey> : IEqualityComparer<TPerson>
		where TPerson : class, IPerson<TKey>
		where TKey : notnull {

		public bool Equals(TPerson? x, TPerson? y) {
			if (x == null && y == null)
				return true;
			if (x == null || y == null)
				return false;

			if (!Equals(x.Id, y.Id))
				return false;

			if (x.FirstName != y.FirstName ||
				x.LastName != y.LastName ||
				x.Email != y.Email ||
				x.PhoneNumber != y.PhoneNumber ||
				x.DateOfBirth != y.DateOfBirth)
				return false;


			// Related entities are unreliable to compare
			// because they are not loaded in the same way

			return true;
		}

        public int GetHashCode([DisallowNull] TPerson obj) => throw new NotSupportedException();
    }
}
