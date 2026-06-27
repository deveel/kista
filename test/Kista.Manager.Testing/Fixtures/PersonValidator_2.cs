using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using System.Runtime.CompilerServices;

namespace Kista {
	public class PersonValidator<TPerson, TKey> : IEntityValidator<TPerson, TKey> 
		where TPerson : class, IPerson<TKey>
		where TKey : notnull {
		public async IAsyncEnumerable<ValidationResult> ValidateAsync(EntityManager<TPerson, TKey> manager, TPerson person, [EnumeratorCancellation] CancellationToken cancellationToken = default) {
			if (person.Email != null && !MailAddress.TryCreate(person.Email, out var _))
				yield return new ValidationResult("The email address is not valid", new[] { nameof(IPerson<TKey>.Email) });

			await Task.CompletedTask;
		}

	}
}
