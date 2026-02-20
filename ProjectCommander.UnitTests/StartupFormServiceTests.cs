using CommunityToolkit.Aspire.ProjectCommander;
using Microsoft.Extensions.Logging;
using Moq;

namespace ProjectCommander.UnitTests;

public class StartupFormServiceTests
{
    private readonly Mock<ILogger<StartupFormService>> _mockLogger;
    private readonly StartupFormService _service;

    public StartupFormServiceTests()
    {
        _mockLogger = new Mock<ILogger<StartupFormService>>();
        _service = new StartupFormService(_mockLogger.Object);
    }

    [Fact]
    public void IsStartupFormRequired_DefaultsToFalse()
    {
        // Assert
        Assert.False(_service.IsStartupFormRequired);
    }

    [Fact]
    public void SetStartupFormRequired_SetsPropertyCorrectly()
    {
        // Act
        _service.SetStartupFormRequired(true);

        // Assert
        Assert.True(_service.IsStartupFormRequired);
    }

    [Fact]
    public void IsStartupFormCompleted_DefaultsToFalse()
    {
        // Assert
        Assert.False(_service.IsStartupFormCompleted);
    }

    [Fact]
    public void CompleteStartupForm_SetsPropertiesCorrectly()
    {
        // Arrange
        var formData = new Dictionary<string, string?>
        {
            { "field1", "value1" },
            { "field2", "value2" }
        };

        // Act
        _service.CompleteStartupForm(formData);

        // Assert
        Assert.True(_service.IsStartupFormCompleted);
        Assert.NotNull(_service.StartupFormData);
        Assert.Equal(2, _service.StartupFormData.Count);
        Assert.Equal("value1", _service.StartupFormData["field1"]);
    }

    [Fact]
    public void CompleteStartupForm_ThrowsWhenFormDataIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _service.CompleteStartupForm(null!));
    }

    [Fact]
    public async Task WaitForStartupFormAsync_ReturnsNullWhenNotRequired()
    {
        // Arrange
        _service.SetStartupFormRequired(false);

        // Act
        var result = await _service.WaitForStartupFormAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task WaitForStartupFormAsync_ReturnsDataWhenAlreadyCompleted()
    {
        // Arrange
        var formData = new Dictionary<string, string?>
        {
            { "field1", "value1" }
        };
        _service.SetStartupFormRequired(true);
        _service.CompleteStartupForm(formData);

        // Act
        var result = await _service.WaitForStartupFormAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(formData, result);
    }

    [Fact]
    public async Task WaitForStartupFormAsync_BlocksUntilFormCompleted()
    {
        // Arrange
        _service.SetStartupFormRequired(true);
        var formData = new Dictionary<string, string?>
        {
            { "field1", "value1" }
        };

        // Act
        var waitTask = _service.WaitForStartupFormAsync();
        
        // Verify the task is not completed yet
        Assert.False(waitTask.IsCompleted);

        // Complete the form
        _service.CompleteStartupForm(formData);

        // Wait a bit for the task to complete
        var result = await waitTask;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(formData, result);
    }

    [Fact]
    public async Task WaitForStartupFormAsync_ThrowsWhenCancelled()
    {
        // Arrange
        _service.SetStartupFormRequired(true);
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _service.WaitForStartupFormAsync(cts.Token));
    }
}
