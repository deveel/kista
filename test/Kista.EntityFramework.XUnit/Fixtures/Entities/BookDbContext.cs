using Microsoft.EntityFrameworkCore;

using System;

namespace Kista.Entities
{
	public class BookDbContext : DbContext
	{
		public BookDbContext(DbContextOptions<BookDbContext> options, IUserAccessor<string> userAccessor) : base(options)
		{
			this.userAccessor = userAccessor;
		}

		private readonly IUserAccessor<string> userAccessor;

		public DbSet<DbBookWithOwner> Books { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<DbBookWithOwner>()
				.HasKey(x => x.Id);

			modelBuilder.Entity<DbBookWithOwner>()
				.HasOwnerFilter(nameof(DbBookWithOwner.UserId), userAccessor);
		}
	}
}
