using IDificator.Entities;

namespace IDificator;
public class DbMaintenance(IServiceProvider serviceProvider, ILogger<DbMaintenance> logger) : BackgroundService
{
    readonly IServiceProvider serviceProvider = serviceProvider;
    readonly ILogger<DbMaintenance> log = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            log.LogInformation("DB maintenance task running...");

            try
            {
                using var scope = serviceProvider.CreateScope();
                log.LogInformation("Start DB maintenance");

                var db = scope.ServiceProvider.GetService<DB>();
                var threads = await db.QueryAsync<(string boardId, string threadId)>("select BoardId, ThreadId from Shitposts group by ThreadId");

                foreach (var t in threads)
                {
                    log.LogInformation("Checking if thread '{thread}' is alive", $"{t.boardId}.{t.threadId}");

                    var resp = await WebRequest.GetRequestAsync<ChanResponse>($"https://a.4cdn.org/{t.boardId}/thread/{t.threadId}.json");
                    if (resp == null || resp.IsArchived)
                    {
                        log.LogInformation("Thread '{thread}' is dead, deleting...", $"{t.boardId}.{t.threadId}");
                        await db.ExecuteAsync($"delete from Shitposts where BoardId = '{t.boardId}' and ThreadId = '{t.threadId}'");
                    }
                    else
                        log.LogInformation("Thread '{thread}' is alive at {postCount} posts", $"{t.boardId}.{t.threadId}", resp.posts.Count);
                }

                log.LogInformation("Finish DB maintenance");

            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to run DB maintenance task");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
