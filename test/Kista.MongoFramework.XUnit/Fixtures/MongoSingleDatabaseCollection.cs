using System;

namespace Kista {
	[CollectionDefinition(nameof(MongoSingleDatabaseCollection))]
	public class MongoSingleDatabaseCollection : ICollectionFixture<MongoSingleDatabase> {

	}
}
