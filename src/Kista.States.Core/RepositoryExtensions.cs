using System;

namespace Kista {
	/// <summary>
	/// Provides extension methods for <see cref="IStateRepository{TEntity, TStatus}"/>
	/// to simplify synchronous operations.
	/// </summary>
	public static class RepositoryExtensions {
		/// <summary>
		/// Synchronously adds a state to the given entity.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of the entity to add the state to.
		/// </typeparam>
		/// <typeparam name="TStatus">
		/// The type of the status to add.
		/// </typeparam>
		/// <param name="repository">
		/// The repository to add the state to.
		/// </param>
		/// <param name="entity">
		/// The entity to add the state to.
		/// </param>
		/// <param name="stateInfo">
		/// The information describing the state to add.
		/// </param>
		public static void AddState<TEntity, TStatus>(this IStateRepository<TEntity, TStatus> repository, TEntity entity, StateInfo<TStatus> stateInfo)
			where TEntity : class, IEntity
			=> repository.AddStateAsync(entity, stateInfo).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// Synchronously adds a state to the given entity.
		/// </summary>
		/// <typeparam name="TStatus">
		/// The type of the status to add.
		/// </typeparam>
		/// <param name="repository">
		/// The repository to add the state to.
		/// </param>
		/// <param name="entity">
		/// The entity to add the state to.
		/// </param>
		/// <param name="stateInfo">
		/// The information describing the state to add.
		/// </param>
		public static void AddState<TStatus>(this IStateRepository<TStatus> repository, IEntity entity, StateInfo<TStatus> stateInfo)
			=> repository.AddStateAsync(entity, stateInfo).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// Synchronously removes a state from the given entity.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of the entity to remove the state from.
		/// </typeparam>
		/// <typeparam name="TStatus">
		/// The type of the status to remove.
		/// </typeparam>
		/// <param name="repository">
		/// The repository to remove the state from.
		/// </param>
		/// <param name="entity">
		/// The entity to remove the state from.
		/// </param>
		/// <param name="stateInfo">
		/// The information describing the state to remove.
		/// </param>
		public static void RemoveState<TEntity, TStatus>(this IStateRepository<TEntity, TStatus> repository, TEntity entity, StateInfo<TStatus> stateInfo)
			where TEntity : class, IEntity
			=> repository.RemoveStateAsync(entity, stateInfo).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// Synchronously removes a state from the given entity.
		/// </summary>
		/// <typeparam name="TStatus">
		/// The type of the status to remove.
		/// </typeparam>
		/// <param name="repository">
		/// The repository to remove the state from.
		/// </param>
		/// <param name="entity">
		/// The entity to remove the state from.
		/// </param>
		/// <param name="stateInfo">
		/// The information describing the state to remove.
		/// </param>
		public static void RemoveState<TStatus>(this IStateRepository<TStatus> repository, IEntity entity, StateInfo<TStatus> stateInfo)
			=> repository.RemoveStateAsync(entity, stateInfo).ConfigureAwait(false).GetAwaiter().GetResult();
	}
}
