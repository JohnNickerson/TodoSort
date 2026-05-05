using AssimilationSoftware.TodoSort.Core.Extensions;

namespace AssimilationSoftware.TodoSort.UnitTests;

public class ExtensionTests
{
    [Fact]
    public void Test_Tokenise_SimpleCsvLine_ReturnsTokens()
    {
        // Arrange
        string line = "token1,token2,token3";

        // Act
        var tokens = line.Tokenise().ToArray();
        // Assert
        Assert.Equal(3, tokens.Length);
        Assert.Equal("token1", tokens[0]);
        Assert.Equal("token2", tokens[1]);
        Assert.Equal("token3", tokens[2]);
    }

    public void Test_Tokenise_With_Quotes_ReturnsTokens()
    {
        // Arrange
        string line = "\"token, with, commas\",token2,\"token3\"";

        // Act
        var tokens = line.Tokenise().ToArray();

        // Assert
        Assert.Equal(3, tokens.Length);
        Assert.Equal("token, with, commas", tokens[0]);
        Assert.Equal("token2", tokens[1]);
        Assert.Equal("token3", tokens[2]);
    }

    [Fact]
    public void Test_Tokenise_URL_And_Title_ReturnsTokens()
    {
        // Arrange
        string line = "\"URL\",\"Title\"";

        // Act
        var tokens = line.Tokenise().ToArray();

        // Assert
        Assert.Equal(2, tokens.Length);
        Assert.Equal("URL", tokens[0]);
        Assert.Equal("Title", tokens[1]);
    }
}