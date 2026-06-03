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

using System.Linq.Expressions;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;

using MongoFramework.Infrastructure.Linq;

namespace Kista {
	/// <summary>
	/// A filter that can be used to filter a <see cref="IQueryable{TEntity}"/>
	/// for entities that are within a given distance from a center point.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of the entity to be filtered.
	/// </typeparam>
	public sealed class MongoGeoDistanceFilter<TEntity> : IQueryableFilter<TEntity> where TEntity : class {
		/// <summary>
		/// Constructs the filter by specifying the field that contains
		/// the location of the entity, the center point and the maximum
		/// distance from the center point.
		/// </summary>
		/// <param name="field">
		/// The expression that identifies the field that contains the
		/// location of the entity.
		/// </param>
		/// <param name="center">
		/// The center point to calculate the distance from.
		/// </param>
		/// <param name="maxDistance">
		/// The maximum distance from the center point.
		/// </param>
		/// <param name="minDistance">
		/// The minimum distance from the center point.
		/// </param>
		public MongoGeoDistanceFilter(Expression<Func<TEntity, object>> field, GeoPoint center, double? maxDistance = null, double? minDistance = null) {
			ArgumentNullException.ThrowIfNull(field);

			LocationField = field;
			Center = center;
			MaxDistance = maxDistance;
			MinDistance = minDistance;
		}

		/// <summary>
		/// Gets the expression that identifies the field that contains
		/// the location of the entity.
		/// </summary>
		public Expression<Func<TEntity, object>> LocationField { get; }

		/// <summary>
		/// Gets the center point to calculate the distance from.
		/// </summary>
		public GeoPoint Center { get; }

		/// <summary>
		/// Gets the maximum distance from the center point.
		/// </summary>
		public double? MaxDistance { get; }

		/// <summary>
		/// Gets the minimum distance from the center point.
		/// </summary>
		public double? MinDistance { get; }

		IQueryable<TEntity> IQueryableFilter<TEntity>.Apply(IQueryable<TEntity> dbSet) {
			var entitySerializer = BsonSerializer.LookupSerializer<TEntity>();
			var keyExpressionField = new ExpressionFieldDefinition<TEntity>(LocationField);
			var keyStringField = keyExpressionField.Render(entitySerializer, BsonSerializer.SerializerRegistry);
			var point = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(new GeoJson2DGeographicCoordinates(Center.Longitude, Center.Latitude));

			const string distanceFieldName = "Distance";

			var geoNearSettings = new BsonDocument
			{
				{ "near", point.ToBsonDocument() },
				{ "key", keyStringField.FieldName },
				{ "distanceField", distanceFieldName }
			};

			if (MaxDistance.HasValue) {
				geoNearSettings.Add("maxDistance", MaxDistance.Value);
			}
			if (MinDistance.HasValue) {
				geoNearSettings.Add("minDistance", MinDistance.Value);
			}

			var stage = new BsonDocument
			{
				{ "$geoNear", geoNearSettings }
			};

			var originalProvider = dbSet.Provider as IMongoFrameworkQueryProvider<TEntity>;
			var provider = new MongoFrameworkQueryProvider<TEntity>(originalProvider, stage);
			return new MongoFrameworkQueryable<TEntity>(provider);
		}
	}
}
