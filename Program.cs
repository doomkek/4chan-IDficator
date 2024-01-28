using System.Net;
using IDificator;
using Microsoft.AspNetCore.Mvc;
using Serilog;

var config = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(config)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton(c => new DB(c.GetRequiredService<ILogger<DB>>(), "data"));

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

var app = builder.Build();
app.UseCors("Default");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.Map("/ping", () => "pong");

app.MapPost("/addPost", async (HttpContext context, [FromServices] DB db, [FromQuery] long threadId, [FromQuery] long postId) =>
{
    string clientIpAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? context.Connection.RemoteIpAddress!.ToString();

    if (string.IsNullOrEmpty(clientIpAddress))
        return new ReturnMessage(500, "Failed to retrieve user IP");

    try
    {
        IPAddress.Parse(clientIpAddress);
    }
    catch (Exception ex)
    {
        Log.Error(ex.ToString());
        return new ReturnMessage(500, "Failed to parse IP address");
    }

    string userHash = Utils.GenerateHash(threadId, clientIpAddress);

    await db.ExecuteAsync("INSERT INTO Shitposts VALUES (@threadId, @postId, @userHash)", new { threadId, postId, userHash });

    return new ReturnMessage(200, userHash);
});

app.MapGet("/getShitposts/{threadId}", async ([FromServices] DB db, [FromRoute] string threadId) => await db.QueryAsync<Shitpost>($"SELECT PostId, UserHash FROM SHITPOSTS WHERE ThreadId = '{threadId}'"));

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