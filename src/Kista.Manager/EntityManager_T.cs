// Copyright 2023-2025 Antonello Provenzano
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.ComponentModel.DataAnnotations;
using Deveel;
using Kista.Caching;


using Microsoft.Extensions.Logging;

namespace Kista {
	/// <summary>
	/// A service that provides a set of operations to manage a specific
	/// entities in a repository.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of the entity managed by the service.
	/// </typeparam>
	public class EntityManager<TEntity> : EntityManager<TEntity, object> where TEntity : class {
		/// <summary>
		/// Constructs the service with the given repository.
		/// </summary>
		/// <param name="repository">
		/// The repository that is providing the data access to the entities.
		/// </param>
		/// <param name="validator">
		/// An optional service used to validate the entity before it is added
		/// or updated in the repository.
		/// </param>
		/// <param name="cache">
		/// An optional service used to cache the entities
		/// for faster access.
		/// </param>
		/// <param name="systemTime">
		/// A service used to get the current system time.
		/// </param>
		/// <param name="errorFactory">
		/// An optional factory used to create errors specific
		/// for the entity manager.
		/// </param>
		/// <param name="services">
		/// The services used to resolve the dependencies of the manager.
		/// </param>
		/// <param name="loggerFactory">
		/// A factory used to create a logger for the manager.
		/// </param>
		public EntityManager(
			IRepository<TEntity> repository, 
			IEntityValidator<TEntity>? validator = null,
			IEntityCache<TEntity>? cache = null,
			ISystemTime? systemTime = null,
			IOperationErrorFactory<TEntity>? errorFactory = null,
			IServiceProvider? services = null, 
			ILoggerFactory? loggerFactory = null) : base(repository, WrapValidator(validator), cache, systemTime, errorFactory, services, loggerFactory) {
		}

		/// <inheritdoc/>
		public override bool IsTrackingChanges {
			get {
				ThrowIfDisposed();

				return (Repository is ITrackingRepository<TEntity> trackingRepository);
			}
		}

		/// <inheritdoc/>
		protected override ITrackingRepository<TEntity, object> TrackingRepository {
			get {
				ThrowIfDisposed();

				if (!(Repository is ITrackingRepository<TEntity> trackingRepository))
					throw new InvalidOperationException("The repository is not tracking changes.");

				return trackingRepository;
			}
		}

		private static IEntityValidator<TEntity, object>? WrapValidator(IEntityValidator<TEntity>? validator) => 
			validator == null ? null : new EntityValidatorWrapper(validator);

		class EntityValidatorWrapper : IEntityValidator<TEntity, object> {
			private readonly IEntityValidator<TEntity> validator;

			public EntityValidatorWrapper(IEntityValidator<TEntity> validator) {
				this.validator = validator;
			}

			/// <inheritdoc/>
			public IAsyncEnumerable<ValidationResult> ValidateAsync(EntityManager<TEntity, object> manager, TEntity entity, CancellationToken cancellationToken = default) 
				=> validator.ValidateAsync((EntityManager<TEntity>) manager, entity, cancellationToken);
		}
	}
}
