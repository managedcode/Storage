using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Shouldly;
using ManagedCode.Storage.Core;
using Xunit;

namespace ManagedCode.Storage.Tests.Core;

/// <summary>
/// StringStream tests that don't depend on problematic components
/// </summary>
public class StringStreamTests
{
    [Fact]
    public void StringStream_EmptyString_ShouldWork()
    {
        // Arrange
        var input = "";
        
        // Act
        using var stream = new StringStream(input);
        
        // Assert
        stream.CanRead.ShouldBeTrue();
        stream.CanSeek.ShouldBeTrue();
        stream.CanWrite.ShouldBeFalse();
        stream.Length.ShouldBe(0);
        stream.Position.ShouldBe(0);
    }

    [Fact]
    public void StringStream_SimpleString_ShouldWork()
    {
        // Arrange
        var input = "Hello";
        
        // Act
        using var stream = new StringStream(input);
        
        // Assert
        stream.Length.ShouldBe(10); // 5 chars * 2 bytes each in old implementation
        stream.ToString().ShouldBe(input);
    }

    [Fact]
    public void StringStream_ReadByte_ShouldWork()
    {
        // Arrange
        var input = "A";
        using var stream = new StringStream(input);
        
        // Act
        var firstByte = stream.ReadByte();
        var secondByte = stream.ReadByte();
        var thirdByte = stream.ReadByte(); // Should be EOF
        
        // Assert
        firstByte.ShouldNotBe(-1);
        secondByte.ShouldNotBe(-1); 
        thirdByte.ShouldBe(-1); // EOF
    }

    [Fact]
    public void Utf8StringStream_EmptyString_ShouldWork()
    {
        // Arrange
        var input = "";
        
        // Act
        using var stream = new Utf8StringStream(input);
        
        // Assert
        stream.CanRead.ShouldBeTrue();
        stream.CanSeek.ShouldBeTrue();
        stream.CanWrite.ShouldBeFalse();
        stream.Length.ShouldBe(0);
        stream.Position.ShouldBe(0);
    }

    [Fact]
    public void Utf8StringStream_SimpleString_ShouldWork()
    {
        // Arrange
        var input = "Hello";
        
        // Act
        using var stream = new Utf8StringStream(input);
        
        // Assert
        stream.Length.ShouldBe(5); // 5 ASCII chars = 5 bytes in UTF-8
        stream.ToString().ShouldBe(input);
    }

    [Fact]
    public void Utf8StringStream_UnicodeString_ShouldWork()
    {
        // Arrange
        var input = "🚀"; // This emoji is 4 bytes in UTF-8
        
        // Act
        using var stream = new Utf8StringStream(input);
        
        // Assert
        stream.Length.ShouldBe(4); // Emoji = 4 bytes in UTF-8
        stream.ToString().ShouldBe(input);
    }

    [Fact]
    public void Utf8StringStream_ReadAllBytes_ShouldMatchOriginal()
    {
        // Arrange
        var input = "Test 123";
        using var stream = new Utf8StringStream(input);
        var expectedBytes = Encoding.UTF8.GetBytes(input);
        
        // Act
        var buffer = new byte[stream.Length];
        var bytesRead = stream.Read(buffer, 0, buffer.Length);
        
        // Assert
        bytesRead.ShouldBe(expectedBytes.Length);
        buffer.ShouldBe(expectedBytes);
    }

    [Fact]
    public async Task Utf8StringStream_ReadAsync_ShouldWork()
    {
        // Arrange
        var input = "Async test";
        using var stream = new Utf8StringStream(input);
        var expectedBytes = Encoding.UTF8.GetBytes(input);
        
        // Act
        var buffer = new byte[stream.Length];
        var bytesRead = await stream.ReadAsync(buffer);
        
        // Assert
        bytesRead.ShouldBe(expectedBytes.Length);
        buffer.ShouldBe(expectedBytes);
    }

    [Fact]
    public void Utf8StringStream_Seek_ShouldWork()
    {
        // Arrange
        var input = "Seek test";
        using var stream = new Utf8StringStream(input);
        
        // Act & Assert
        stream.Seek(0, SeekOrigin.Begin).ShouldBe(0);
        stream.Position.ShouldBe(0);
        
        stream.Seek(5, SeekOrigin.Begin).ShouldBe(5);
        stream.Position.ShouldBe(5);
        
        stream.Seek(0, SeekOrigin.End).ShouldBe(stream.Length);
        stream.Position.ShouldBe(stream.Length);
    }

    [Fact]
    public void Utf8StringStream_WriteOperations_ShouldThrow()
    {
        // Arrange
        using var stream = new Utf8StringStream("test");
        var buffer = new byte[5];
        
        // Act & Assert
        var act1 = () => stream.Write(buffer, 0, buffer.Length);
        Should.Throw<NotSupportedException>(act1);

        var act2 = () => stream.SetLength(100);
        Should.Throw<NotSupportedException>(act2);
    }

    [Fact]
    public void Utf8StringStream_ExtensionMethods_ShouldWork()
    {
        // Arrange
        var input = "Extension test";
        
        // Act
        using var stream1 = input.ToUtf8Stream();
        using var stream2 = Encoding.UTF8.GetBytes(input).ToUtf8Stream();
        
        // Assert
        stream1.ToString().ShouldBe(input);
        stream2.ToString().ShouldBe(input);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("Hello, World!")]
    [InlineData("🚀🌍💻")]
    [InlineData("Mixed: English + Українська")]
    public void Utf8StringStream_VariousInputs_ShouldPreserveContent(string input)
    {
        // Act
        using var stream = new Utf8StringStream(input);
        
        // Assert
        stream.ToString().ShouldBe(input);
        stream.Length.ShouldBe(Encoding.UTF8.GetByteCount(input));
    }

    [Fact]
    public void StringStreams_MemoryComparison_Utf8ShouldBeMoreEfficient()
    {
        // Arrange
        var input = "Memory test 🚀"; // Contains Unicode
        
        // Act
        using var oldStream = new StringStream(input);
        using var newStream = new Utf8StringStream(input);
        
        // Assert
        newStream.Length.ShouldBeLessThanOrEqualTo(oldStream.Length);
        oldStream.ToString().ShouldBe(newStream.ToString());
    }
}
