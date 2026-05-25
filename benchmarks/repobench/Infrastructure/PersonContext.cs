using Kista.Benchmarks.Models;

using Microsoft.EntityFrameworkCore;

namespace Kista.Benchmarks.Infrastructure;

public class PersonContext(string connectionString) : DbContext {
	public DbSet<EfBenchPerson> People => Set<EfBenchPerson>();

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		=> optionsBuilder.UseMySQL(connectionString);
}

