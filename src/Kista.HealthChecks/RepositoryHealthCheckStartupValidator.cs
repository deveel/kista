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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kista.HealthChecks;

/// <summary>
/// Hosted service that validates repository health at startup.
/// </summary>
internal sealed class RepositoryHealthCheckStartupValidator : IHostedService {
    private readonly IServiceProvider _serviceProvider;
    private readonly RepositoryHealthCheckOptions _options;
    private readonly ILogger<RepositoryHealthCheckStartupValidator> _logger;
    
    public RepositoryHealthCheckStartupValidator(
        IServiceProvider serviceProvider,
        RepositoryHealthCheckOptions options,
        ILogger<RepositoryHealthCheckStartupValidator> logger) {
        _serviceProvider = serviceProvider;
        _options = options;
        _logger = logger;
    }
    
    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken) {
        if (_options.StartupValidationMode == StartupValidationMode.None)
            return;
        
        _logger.LogStartingValidation();
        
        try {
            var healthCheckService = _serviceProvider.GetRequiredService<HealthCheckService>();
            var report = await healthCheckService.CheckHealthAsync(cancellationToken);
            
            if (report.Status != HealthStatus.Healthy) {
                var status = report.Status.ToString();
                var message = $"Repository health checks failed at startup: {status}";
                
                if (_options.StartupValidationMode == StartupValidationMode.FailFast) {
                    _logger.LogFailedStartup(status);
                    throw new InvalidOperationException(message);
                }
                
                _logger.LogFailedWarning(status);
                
                foreach (var entry in report.Entries.Where(e => e.Value.Status != HealthStatus.Healthy)) {
                    _logger.LogCheckFailed(entry.Key, entry.Value.Status, entry.Value.Description);
                }
            }
            else {
                _logger.LogPassedValidation();
            }
        }
        catch (Exception ex) {
            _logger.LogValidationException(ex);
            
            if (_options.StartupValidationMode == StartupValidationMode.FailFast)
                throw;
        }
    }
    
    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
