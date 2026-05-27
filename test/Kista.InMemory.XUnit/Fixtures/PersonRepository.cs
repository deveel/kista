namespace Kista {
	public class PersonRepository : InMemoryRepository<Person> {
		public PersonRepository() 
			: this(Array.Empty<Person>()) {
		}

		internal PersonRepository(IList<Person>? entities) 
			: base(entities) {
		}
	}
}
