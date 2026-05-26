namespace Kista {
	public class PersonRepository : InMemoryRepository<Person> {
		public PersonRepository() 
			: base(Enumerable.Empty<Person>()) {
		}

		internal PersonRepository(IList<Person>? entities = null) 
			: base(entities) {
		}
	}
}
