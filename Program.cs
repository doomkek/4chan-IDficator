using System.Net;
using System.Security.AccessControl;
using IDificator;
using IDificator.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters.Xml;
using Serilog;
using WR = IDificator.WebRequest;

var config = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build();

var appConfig = config.GetSection("AppConfig").Get<AppConfig>();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(config)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton(c => new DB(c.GetRequiredService<ILogger<DB>>(), "data"));
builder.Services.AddSingleton<AppConfig>(appConfig);

builder.Host.UseSerilog();
builder.Services.AddCors(options =>
{
    options.DefaultPolicyName = "Default";
    options.AddPolicy("Default", builder =>
    {
        builder.WithOrigins("https://boards.4chan.org")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

builder.Services.AddHostedService<DbMaintenance>();
builder.Services.AddHostedService<PostValidator>();

var app = builder.Build();
app.UseCors("Default");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.MapPost("/addPost", async (HttpContext context, [FromServices] DB db, [FromQuery] long threadId, [FromQuery] long postId, [FromQuery] string boardId = "vg") =>
{
    string clientIpAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? context.Connection.RemoteIpAddress!.ToString();

    string userToken = Utils.GenerateToken(threadId, clientIpAddress, appConfig.Secret);
    string userHash = Utils.GenerateHash(threadId, userToken);

    try
    {
        await db.ExecuteAsync("INSERT INTO ValidationQueue VALUES (@boardId, @threadId, @postId, @serverTS, @attempt)", new { boardId, threadId, postId, serverTS = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), attempt = 0 });
        await db.ExecuteAsync("INSERT INTO Shitposts VALUES (@threadId, @postId, @boardId, @userHash)", new { threadId, postId, boardId, userHash });
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to execute SQL in /appPost");
        return new ReturnMessage(500, "");
    }

    return new ReturnMessage(200, userHash);
});

app.MapGet("/getShitposts/{threadId}", async ([FromServices] DB db, [FromRoute] string threadId) => await db.QueryAsync<Shitpost>($"SELECT PostId, UserHash FROM SHITPOSTS WHERE BoardId = @boardId and ThreadId = @threadId", new { boardId = "vg", threadId }));
app.MapGet("/getShitposts/{boardId}/{threadId}", async ([FromServices] DB db, [FromRoute] string boardId, [FromRoute] string threadId) => await db.QueryAsync<Shitpost>($"SELECT PostId, UserHash FROM SHITPOSTS WHERE BoardId = @boardId and ThreadId = @threadId", new { boardId, threadId }));

try
{
    Log.Information("Starting web host");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

record ReturnMessage(int StatusCode, string Message);
record Shitpost(long PostId, string UserHash);