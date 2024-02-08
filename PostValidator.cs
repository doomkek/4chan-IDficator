using IDificator.Entities;

namespace IDificator;
public class PostValidator(IServiceProvider serviceProvider, ILogger<PostValidator> logger, AppConfig config) : BackgroundService
{
    readonly IServiceProvider serviceProvider = serviceProvider;
    readonly ILogger<PostValidator> log = logger;
    readonly AppConfig config = config;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            log.LogInformation("Post Validation task running...");

            try
            {
                using var scope = serviceProvider.CreateScope();
                log.LogInformation("Start Post Validation");

                var db = scope.ServiceProvider.GetService<DB>();
                var posts = await db.QueryAsync<ValidationQueue>("select * from ValidationQueue");
                var threads = posts.GroupBy(t => new { t.BoardId, t.ThreadId });

                foreach (var thread in threads)
                {
                    var resp = await WebRequest.GetRequestAsync<ChanResponse>($"https://a.4cdn.org/{thread.Key.BoardId}/thread/{thread.Key.ThreadId}.json");
                    if (resp == null || resp.IsArchived)
                    {
                        await db.ExecuteAsync("delete from ValidationQueue where BoardId = @BoardId and ThreadId = @ThreadId", new { thread.Key.BoardId, thread.Key.ThreadId });
                        await db.ExecuteAsync("delete from Shitposts where BoardId = @BoardId and ThreadId = @ThreadId", new { thread.Key.BoardId, thread.Key.ThreadId });
                        log.LogInformation("Thread '{boardId}.{threadId}' not found, related posts deleted", thread.Key.BoardId, thread.Key.ThreadId);
                        continue;
                    }

                    foreach (var post in thread)
                    {
                        var p = resp.posts.SingleOrDefault(p => p.no == post.PostId);
                        if (p == null)
                        {
                            if (post.Attempt >= config.PostValidationMaxAttempt)
                            {
                                log.LogInformation("Post {postId} not found, validation attempts limit reached, deleting post", post.FullPostId);
                                await db.ExecuteAsync("delete from Shitposts where BoardId = @BoardId and ThreadId = @ThreadId and PostId = @PostId", new { post.BoardId, post.ThreadId, post.PostId });
                                await db.ExecuteAsync("delete from ValidationQueue where BoardId = @BoardId and ThreadId = @ThreadId and PostId = @PostId", new { post.BoardId, post.ThreadId, post.PostId });
                            }
                            else
                            {
                                log.LogInformation("Post {postId} not found, attempt [{attempt}/{attemptMax}] ", post.FullPostId, post.Attempt, config.PostValidationMaxAttempt);
                                await db.ExecuteAsync("update ValidationQueue set Attempt = @Attempt where BoardId = @BoardId and ThreadId = @ThreadId and PostId = @PostId", new { Attempt = (post.Attempt + 1), post.BoardId, post.ThreadId, post.PostId });
                            }
                        }
                        else
                        {
                            log.LogInformation("Velidating post {postId}, post DT {postDT:yyyy/MM/dd HH:mm:ss}, server DT {serverDT:yyyy/MM/dd HH:mm:ss}", post.FullPostId, post.PostDateTime(p.time), post.ServerDateTime);

                            if (post.ServerDateTime - post.PostDateTime(p.time) > TimeSpan.FromSeconds(config.PostValidMaxSpan))
                            {
                                await db.ExecuteAsync("delete from Shitposts where BoardId = @BoardId and ThreadId = @ThreadId and PostId = @PostId", new { post.BoardId, post.ThreadId, post.PostId });
                                await db.ExecuteAsync("delete from ValidationQueue where BoardId = @BoardId and ThreadId = @ThreadId and PostId = @PostId", new { post.BoardId, post.ThreadId, post.PostId });
                                log.LogInformation("Post {postId} validation failed, time difference is {diff}, max allowed {maxDiff}, post deleted", post.FullPostId, post.ServerDateTime - post.PostDateTime(p.time), config.PostValidMaxSpan);
                            }
                            else
                            {
                                await db.ExecuteAsync("delete from ValidationQueue where BoardId = @BoardId and ThreadId = @ThreadId and PostId = @PostId", new { post.BoardId, post.ThreadId, post.PostId });
                                log.LogInformation("Post {postId} validated, deleted from validation queue", post.FullPostId);
                            }
                        }
                    }
                }

                log.LogInformation("Finish Post Validation");

            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to run Post Validation task");
            }

            await Task.Delay(TimeSpan.FromSeconds(config.PostValidationTaskInterval), stoppingToken);
        }
    }
}
