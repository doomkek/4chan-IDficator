namespace IDificator;
public class DbMaintenance : BackgroundService
{
    IServiceProvider serviceProvider;
    ILogger<DbMaintenance> log;
    public DbMaintenance(IServiceProvider serviceProvider, ILogger<DbMaintenance> logger)
    {
        this.serviceProvider = serviceProvider;
        this.log = logger;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            log.LogInformation("DB maintenance task running...");

            try
            {
                using (var scope = serviceProvider.CreateScope())
                {
                    log.LogInformation("Start DB maintenance");

                    var db = scope.ServiceProvider.GetService<DB>();
                    await db.ExecuteAsync("DELETE FROM Shitposts WHERE ThreadId NOT IN (SELECT ThreadId FROM Shitposts GROUP BY ThreadId ORDER BY ThreadId DESC LIMIT 3)");

                    log.LogInformation("Finish DB maintenance");
                }

            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to run DB maintenance task");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}