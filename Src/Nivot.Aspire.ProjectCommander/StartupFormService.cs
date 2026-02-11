using Microsoft.Extensions.Logging;

namespace CommunityToolkit.Aspire.ProjectCommander;

/// <summary>
/// Default implementation of the startup form service.
/// </summary>
internal sealed class StartupFormService : IStartupFormService
{
    private readonly ILogger<StartupFormService> _logger;
    private readonly TaskCompletionSource<Dictionary<string, string?>> _completionSource = new();
    
    private bool _isStartupFormRequired;
    private bool _isStartupFormCompleted;
    private Dictionary<string, string?>? _startupFormData;

    public StartupFormService(ILogger<StartupFormService> logger)
    {
        _logger = logger;
    }

    public bool IsStartupFormRequired => _isStartupFormRequired;

    public bool IsStartupFormCompleted => _isStartupFormCompleted;

    public Dictionary<string, string?>? StartupFormData => _startupFormData;

    public void SetStartupFormRequired(bool required)
    {
        _isStartupFormRequired = required;
        _logger.LogDebug("Startup form required set to: {Required}", required);
    }

    public void CompleteStartupForm(Dictionary<string, string?> formData)
    {
        if (formData == null)
        {
            throw new ArgumentNullException(nameof(formData));
        }

        _isStartupFormCompleted = true;
        _startupFormData = formData;
        _completionSource.TrySetResult(formData);
        _logger.LogInformation("Startup form completed with {Count} fields", formData.Count);
    }

    public async Task<Dictionary<string, string?>?> WaitForStartupFormAsync(CancellationToken cancellationToken = default)
    {
        // If no startup form is required, return immediately
        if (!_isStartupFormRequired)
        {
            _logger.LogDebug("No startup form required, continuing immediately");
            return null;
        }

        // If already completed, return the data
        if (_isStartupFormCompleted && _startupFormData != null)
        {
            return _startupFormData;
        }

        _logger.LogInformation("Waiting for startup form to be completed...");

        // Wait for the form to be completed
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var completedTask = await Task.WhenAny(
            _completionSource.Task,
            Task.Delay(Timeout.Infinite, cts.Token));

        if (completedTask == _completionSource.Task)
        {
            return await _completionSource.Task;
        }

        // Cancelled
        cancellationToken.ThrowIfCancellationRequested();
        return null;
    }
}
