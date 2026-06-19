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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoFramework;

namespace Kista.HealthChecks;

/// <summary>
/// Health check implementation for MongoDB repositories.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TKey">The key type.</typeparam>
public class MongoHealthCheck<TEntity, TKey> : RepositoryHealthCheckBase<TEntity, TKey>
    where TEntity : class {
    
    private readonly MongoHealthCheckOptions _options;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="MongoHealthCheck{TEntity, TKey}"/> class.
    /// </summary>
    /// <param name="options">The health check options.</param>
    public MongoHealthCheck(IOptions<MongoHealthCheckOptions> options) {
        _options = options?.Value ?? new MongoHealthCheckOptions();
    }
    
    /// <inheritdoc/>
    public override string DriverType => "MongoDB";
    
    /// <inheritdoc/>
    protected override async ValueTask<HealthCheckResult> CheckHealthAsyncCore(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken) {
        
        var context = serviceProvider.GetRequiredService<IMongoDbContext>();
        
        try {
            using var pingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            pingCts.CancelAfter(_options.PingTimeout);
            
            var pingCommand = new BsonDocument("ping", 1);
            var database = context.Connection.GetDatabase();
            var result = await database.RunCommandAsync<BsonDocument>(
                pingCommand, 
                cancellationToken: pingCts.Token);
            
            return HealthCheckResult.Healthy(
                "MongoDB connection successful",
                data: CreateDiagnosticData(
                    KeyValuePair.Create<string, object?>("ResponseType", "Healthy")));
        }
        catch (Exception ex) {
            return HealthCheckResult.Unhealthy(
                $"MongoDB connection failed: {ex.Message}",
                exception: ex,
                data: CreateDiagnosticData(
                    KeyValuePair.Create<string, object?>("ExceptionType", ex.GetType().FullName)));
        }
    }
}
