using System.Security.Cryptography;
using StackExchange.Redis;

namespace WorkerService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public Worker(
        ILogger<Worker> logger,
        IConnectionMultiplexer connectionMultiplexer)
    {
        _logger = logger;
        _connectionMultiplexer = connectionMultiplexer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var key = Guid.NewGuid().ToString();
            var value = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            var database = _connectionMultiplexer.GetDatabase();

            var isPersisted = await database.SetAddAsync(key, value);
            if (isPersisted)
            {
                _logger.LogInformation("Persisted {Key} as {Value}", key, value);
                var isDeleted = await database.KeyDeleteAsync(key);

                if (isDeleted)
                {
                    _logger.LogInformation("Successfully deleted {Key}", key);
                }
                else
                {
                    _logger.LogWarning("Failed deleting {Key}", key);
                }
            }
            else
            {
                _logger.LogWarning("Failed persisting {Key}", key);
            }

            await Task.Delay(1000, stoppingToken);
        }
    }
}