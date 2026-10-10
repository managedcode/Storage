using System.IO;
using ManagedCode.Storage.Core.Helpers;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Core;

public sealed class PathHelperContractsTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("folder\\nested/file.txt", "folder/nested/file.txt")]
    public void NormalizePaths_ProducesTheRequestedSeparator(string? input, string expected)
    {
        PathHelper.ToUnixPath(input).ShouldBe(expected);
        PathHelper.ToWindowsPath(input).ShouldBe(expected.Replace('/', '\\'));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("/root", true)]
    [InlineData("\\root", true)]
    [InlineData("C:\\root", true)]
    [InlineData("1:relative", false)]
    [InlineData("relative", false)]
    public void IsAbsolutePath_DistinguishesPortableRoots(string? path, bool expected)
    {
        PathHelper.IsAbsolutePath(path).ShouldBe(expected);
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("file.txt", "file.txt")]
    [InlineData("folder\\nested/file.txt", "file.txt")]
    [InlineData("folder/", "")]
    public void GetFileName_HandlesMixedSeparators(string? path, string expected)
    {
        PathHelper.GetFileName(path).ShouldBe(expected);
    }

    [Fact]
    public void CombinePaths_HandlesEmptySegmentsAndRoot()
    {
        PathHelper.CombineUnixPaths().ShouldBeEmpty();
        PathHelper.CombineUnixPaths(null!).ShouldBeEmpty();
        PathHelper.CombineUnixPaths("/", "", "/folder", "\\file.txt").ShouldBe("/folder/file.txt");
        PathHelper.CombineWindowsPaths("C:", "folder", "file.txt").ShouldBe("C:\\folder\\file.txt");
        PathHelper.CombineUnixPaths(null!, "folder", "file.txt").ShouldBe("folder/file.txt");
    }

    [Theory]
    [InlineData(null, "", "/", "")]
    [InlineData("", "", "/", "")]
    [InlineData("/", "", "/", "/")]
    [InlineData("\\folder\\file/", "folder\\file/", "/folder/file/", "\\folder\\file")]
    public void RootAndTrailingSeparatorHelpers_PreserveRootContracts(
        string? input, string relative, string absolute, string trimmed)
    {
        PathHelper.EnsureRelativePath(input).ShouldBe(relative);
        PathHelper.EnsureAbsolutePath(input).ShouldBe(absolute);
        PathHelper.TrimTrailingSeparators(input).ShouldBe(trimmed);
    }

    [Fact]
    public void DirectoryHelpers_ExtractPlatformPathsAndNormalizeOutput()
    {
        var path = Path.Combine("folder", "nested", "file.txt");
        PathHelper.GetUnixDirectoryPath(path).ShouldBe("folder/nested");
        PathHelper.GetDirectoryPath(path, '\\').ShouldBe("folder\\nested");
        PathHelper.GetDirectoryPath(null).ShouldBeEmpty();
        PathHelper.GetDirectoryPath("").ShouldBeEmpty();
    }
}
