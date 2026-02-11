using CommunityToolkit.Aspire.Hosting.ProjectCommander;

namespace ProjectCommander.UnitTests;

public class ResourceNameParserTests
{
    private readonly ResourceNameParser _parser;

    public ResourceNameParserTests()
    {
        _parser = new ResourceNameParser();
    }

    [Theory]
    [InlineData("datagenerator-abc123", "datagenerator")]
    [InlineData("consumer-xyz789", "consumer")]
    [InlineData("my-service-12345", "my")]
    [InlineData("singlename", "singlename")]
    [InlineData("resource-with-multiple-hyphens-123", "resource")]
    public void GetBaseResourceName_ParsesCorrectly(string input, string expected)
    {
        // Act
        var result = _parser.GetBaseResourceName(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void GetBaseResourceName_ThrowsForInvalidInput(string? input)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _parser.GetBaseResourceName(input!));
    }
}
