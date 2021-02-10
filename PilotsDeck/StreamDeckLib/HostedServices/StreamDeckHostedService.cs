using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StreamDeckLib.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace StreamDeckLib.HostedServices
{
  public class StreamDeckHostedService : IHostedService, IAsyncDisposable
	{
		private readonly ILogger<StreamDeckHostedService> _logger;
		private readonly ConnectionManager _connectionManager;
		private readonly StreamDeckRegistrationOptions _options;
		public StreamDeckHostedService(ILogger<StreamDeckHostedService> logger, ConnectionManager connectionManager, IOptions<StreamDeckRegistrationOptions> options)
		{
			_logger = logger;
			_connectionManager = connectionManager;
			_options = options.Value;
		}

		public ValueTask DisposeAsync()
		{
			return new ValueTask(Task.CompletedTask);
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			_logger?.LogInformation("StreamDeckHostedService: Starting");
			_connectionManager.RegisterAllActions(_options.Assembly);
			await Task.Factory.StartNew(async () => await _connectionManager.StartAsync(), TaskCreationOptions.LongRunning);
			_logger?.LogInformation("StreamDeckHostedService: Started");
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			_logger?.LogInformation("StreamDeckHostedService: Stopping");
			_logger?.LogInformation("StreamDeckHostedService: Stopped");
			return Task.CompletedTask;
		}
	}
}
