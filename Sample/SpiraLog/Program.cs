// See https://aka.ms/new-console-template for more information

using Microsoft.AspNetCore.SignalR.Client;
using Spectre.Console;

AnsiConsole.MarkupLine("[green]SpiraLog[/] 0.1");
AnsiConsole.WriteLine();

if (args.Length == 0)
{
    AnsiConsole.WriteLine("Usage: spiralog <resourceName> [hubPort]");
    return;
}

string resourceName = args[0];

int? port = args.Length == 2 ? int.Parse(args[1]) : null;

//var builder = Host.CreateApplicationBuilder(args);

var hubBuilder = new HubConnectionBuilder()
    .WithUrl($"https://localhost:{port ?? 27960}/projectcommander/")
    .WithAutomaticReconnect()
    .WithKeepAliveInterval(TimeSpan.FromSeconds(120)); // two minute keep alive
    
await using var hubConnection = hubBuilder.Build();

await hubConnection.StartAsync();

AnsiConsole.MarkupLine($"[green]Connected[/] to Aspire Project Commander Hub for resource '[yellow]{resourceName}[/]'");
AnsiConsole.MarkupLine("Press [bold]SPACE[/] to pause/resume log output");

// Simple pause mechanism
bool isPaused = false;
var pauseEvent = new ManualResetEventSlim(true); // Initially not paused
var cts = new CancellationTokenSource();

// Start a task to monitor for space bar presses
_ = Task.Run(() =>
{
    try
    {
        while (!cts.Token.IsCancellationRequested)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Spacebar)
                {
                    isPaused = !isPaused;
                    
                    if (isPaused)
                    {
                        pauseEvent.Reset(); // Block processing
                        AnsiConsole.MarkupLine("[bold red]PAUSED[/] (Press SPACE to resume)");
                    }
                    else
                    {
                        pauseEvent.Set(); // Allow processing to continue
                        AnsiConsole.MarkupLine("[bold green]RESUMED[/]");
                    }
                }
            }
            Thread.Sleep(50);
        }
    }
    catch (Exception ex)
    {
        AnsiConsole.MarkupLine($"[red]Error in key monitoring:[/] {ex.Message}");
    }
}, cts.Token);

try
{
    await foreach (var data in hubConnection.StreamAsync<IReadOnlyList<LogLine>>("WatchResourceLogs", resourceName, null))
    {
        // Wait here if paused, but respect cancellation
        pauseEvent.Wait(cts.Token);
        
        foreach (var (lineNumber, content, isErrorMessage) in data)
        {
            // Check if we got paused during processing, with cancellation support
            pauseEvent.Wait(cts.Token);
            
            // parse out timestamp
            var parts = content.Split(' ', 2);
            var timestamp = parts[0];
            var compactTimestamp = DateTimeOffset.Parse(timestamp).ToLocalTime().ToString("HH:mm:ss.fff");

            AnsiConsole.Markup($"[yellow][[{compactTimestamp}]][/] ");

            if (isErrorMessage)
            {
                AnsiConsole.Write("[red]*[/] ");

                // may contain embedded escape codes
                AnsiConsole.WriteLine(parts.Length > 1 ? parts[1] : string.Empty);
            }
            else
            {
                // may contain embedded escape codes
                AnsiConsole.WriteLine(parts.Length > 1 ? parts[1] : string.Empty);
            }
        }
    }
}
finally
{
    // shutdown key monitoring task
    cts.Cancel();
}

public readonly record struct LogLine(int LineNumber, string Content, bool IsErrorMessage);


