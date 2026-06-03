// Copyright 2023-2026 Antonello Provenzano
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
using System.Linq.Expressions;
using System.Reflection;

namespace Kista {
	/// <summary>
	/// Wraps an enumerable of entities as a repository implementation.
	/// </summary>
	/// <typeparam name="TEntity">The type of entity handled by the repository.</typeparam>
	class RepositoryWrapper<TEntity> : 
		IRepository<TEntity>,
		IFilterableRepository<TEntity>,
		IQueryableRepository<TEntity>
		where TEntity : class {
		private readonly IEnumerable<TEntity> entities;
		private MemberInfo? idMember;

		public RepositoryWrapper(IEnumerable<TEntity> entities) {
			this.entities = entities ?? throw new ArgumentNullException(nameof(entities));
		}

		private bool IsMutable => (entities is IList<TEntity> list && !list.IsReadOnly) || 
			(entities is ICollection<TEntity> collection && !collection.IsReadOnly);

		private void AssertMutable() {
			if (!IsMutable)
				throw new NotSupportedException("The repository is readonly");
		}

		private void AddEntity(TEntity entity) {
			if (entities is IList<TEntity> list) {
				list.Add(entity);
			} else if (entities is ICollection<TEntity> collection) {
				collection.Add(entity);
			} else {
				throw new NotSupportedException("The repository is readonly");
			}
		}

		/// <inheritdoc/>
		public IQueryable<TEntity> AsQueryable() => entities.AsQueryable();

		private MemberInfo DiscoverKeyMember() {
			if (idMember == null) {
				var idMembers = typeof(TEntity).GetMembers(BindingFlags.Instance | BindingFlags.Public)
					.Where(x => x.MemberType == MemberTypes.Property || x.MemberType == MemberTypes.Field)
					.Where(x => Attribute.IsDefined(x, typeof(KeyAttribute)))
					.ToList();

				if (idMembers.Count == 0)
					throw new NotSupportedException("The entity does not have an Id property");

				if (idMembers.Count > 1)
					throw new NotSupportedException("The entity has more than one Id property");

				idMember = idMembers[0];
			}

			return idMember;
		}

		/// <inheritdoc/>
		public object? GetEntityKey(TEntity entity) {
			var member = DiscoverKeyMember();

			if (member is PropertyInfo propertyInfo) {
				return propertyInfo.GetValue(entity);
			} else if (member is FieldInfo fieldInfo) {
				return fieldInfo.GetValue(entity);
			} else {
				throw new NotSupportedException("The entity Id is not supported");
			}
		}

		/// <inheritdoc/>
		public ValueTask AddAsync(TEntity entity, CancellationToken cancellationToken = default) {
			AssertMutable();

			var id = SetId(entity);

			AddEntity(entity);

			return ValueTask.CompletedTask;
		}

		private string SetId(TEntity entity) {
			var member = DiscoverKeyMember();
			var entityId = Guid.NewGuid().ToString("N");

			if (member is PropertyInfo propertyInfo) {
				propertyInfo.SetValue(entity, entityId);
			} else if (member is FieldInfo fieldInfo) {
				fieldInfo.SetValue(entity, entityId);
			} else {
				throw new NotSupportedException("The entity Id is not supported");
			}

			return entityId;
		}

		/// <inheritdoc/>
		public ValueTask AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) {
			AssertMutable();

			foreach (var entity in entities) {
				var id = SetId(entity);
				AddEntity(entity);
			}

			return ValueTask.CompletedTask;
		}

		/// <inheritdoc/>
		public ValueTask<TEntity?> FindAsync(object key, CancellationToken cancellationToken = default) {
			var entity = entities.FirstOrDefault(x => GetEntityKey(x) == key);
			return new ValueTask<TEntity?>(entity);
		}

		/// <inheritdoc/>
		public ValueTask<bool> RemoveAsync(TEntity entity, CancellationToken cancellationToken = default) {
			AssertMutable();

			var key = GetEntityKey(entity);
			if (key == null)
				return new ValueTask<bool>(false);

			var found = entities.FirstOrDefault(x => GetEntityKey(x) == key);
			if (found == null)
				return new ValueTask<bool>(false);

			if (entities is IList<TEntity> list) {
				list.Remove(found);
			} else if (entities is ICollection<TEntity> collection) {
				collection.Remove(found);
			} else {
				throw new NotSupportedException("The repository is readonly");
			}

			return new ValueTask<bool>(true);
		}

		/// <inheritdoc/>
		public ValueTask RemoveRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) {
			AssertMutable();

			foreach (var entity in entities) {
				var key = GetEntityKey(entity);
				if (key == null)
					continue;

				var found = this.entities.FirstOrDefault(x => GetEntityKey(x) == key);
				if (found == null)
					continue;

				if (this.entities is IList<TEntity> list) {
					list.Remove(found);
				} else if (this.entities is ICollection<TEntity> collection) {
					collection.Remove(found);
				} else {
					throw new NotSupportedException("The repository is readonly");
				}
			}

			return ValueTask.CompletedTask;
		}

		/// <inheritdoc/>
		public ValueTask<bool> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default) {
			AssertMutable();

			var key = GetEntityKey(entity);
			if (key == null)
				return new ValueTask<bool>(false);

			var found = entities.FirstOrDefault(x => GetEntityKey(x) == key);
			if (found == null)
				return new ValueTask<bool>(false);

			if (entities is IList<TEntity> list) {
				var index = list.IndexOf(found);
				list[index] = entity;
			} else if (entities is ICollection<TEntity> collection) {
				collection.Remove(found);
				collection.Add(entity);
			} else {
				throw new NotSupportedException("The repository is readonly");
			}

			return new ValueTask<bool>(true);
		}

		/// <inheritdoc/>
		public ValueTask<TEntity?> FindFirstAsync(IQuery query, CancellationToken cancellationToken = default) {
			TEntity? result;

			if (entities is IQueryable<TEntity> queryable) {
				result = query.Apply(queryable).FirstOrDefault();
			} else {
				if (query.HasFilter()) {
					result = entities.Where(query.Filter!.AsLambda<TEntity>().Compile()).FirstOrDefault();
				} else {
					result = entities.FirstOrDefault();
				}
			}

			return new ValueTask<TEntity?>(result);
		}

		/// <inheritdoc/>
		public ValueTask<IList<TEntity>> FindAllAsync(IQuery query, CancellationToken cancellationToken = default) {
			IEnumerable<TEntity> result;

			if (entities is IQueryable<TEntity> queryable) {
				result = query.Apply(queryable);
			} else {
				if (query.HasFilter()) {
					result = entities.Where(query.Filter!.AsLambda<TEntity>().Compile());
				} else {
					result = entities;
				}

				if (query.Order != null) {
					// TODO:
				}
			}

			return new ValueTask<IList<TEntity>>(result.ToList());
		}

		/// <inheritdoc/>
		public ValueTask<bool> ExistsAsync(IQueryFilter filter, CancellationToken cancellationToken = default) {
			bool result;

			if (entities is IQueryable<TEntity> queryable) {
				result = queryable.Any(filter.AsLambda<TEntity>());
			} else {
				result = entities.Any(filter.AsLambda<TEntity>().Compile());
			}

			return new ValueTask<bool>(result);
		}

		/// <inheritdoc/>
		public ValueTask<long> CountAsync(IQueryFilter filter, CancellationToken cancellationToken = default) {
			long result;

			if (entities is IQueryable<TEntity> queryable) {
				result = queryable.LongCount(filter.AsLambda<TEntity>());
			} else {
				result = entities.LongCount(filter.AsLambda<TEntity>().Compile());
			}

			return new ValueTask<long>(result);
		}

		/// <inheritdoc/>
		public ValueTask<PageResult<TEntity>> GetPageAsync(PageRequest request, CancellationToken cancellationToken = default) {
			if (request is PageQuery<TEntity> pageQuery) {
				var queryable = pageQuery.ApplyQuery(entities.AsQueryable());
				var total = queryable.Count();
				var items = queryable
					.Skip(request.Offset)
					.Take(request.Size)
					.ToList();

				return new ValueTask<PageResult<TEntity>>(new PageResult<TEntity>(request, total, items));
			}

			var allItems = entities
				.Skip(request.Offset)
				.Take(request.Size)
				.ToList();

			return new ValueTask<PageResult<TEntity>>(new PageResult<TEntity>(request, entities.Count(), allItems));
		}
	}
}
