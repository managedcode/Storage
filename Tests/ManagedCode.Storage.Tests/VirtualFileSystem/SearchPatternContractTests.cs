using System;
using ManagedCode.Storage.VirtualFileSystem.Options;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.VirtualFileSystem;

public sealed class SearchPatternContractTests
{
    [Theory]
    [InlineData("*.txt", "notes.txt", true)]
    [InlineData("*.txt", "NOTES.TXT", true)]
    [InlineData("*.txt", "notes.csv", false)]
    [InlineData("report-??.txt", "report-12.txt", true)]
    [InlineData("report-??.txt", "report-1.txt", false)]
    [InlineData("a*b*c", "axxbxxbc", true)]
    [InlineData("a*b*c", "axxbxxbd", false)]
    [InlineData("a**", "abc", true)]
    [InlineData("a?c", "abc", true)]
    [InlineData("abc", "abcd", false)]
    [InlineData("abc", "abc", true)]
    [InlineData("", "abc", false)]
    [InlineData("*", "", false)]
    [InlineData("*", null, false)]
    public void IsMatch_RespectsWildcardAndMissingNameRules(string pattern, string? name, bool expected)
    {
        new SearchPattern(pattern).IsMatch(name!).ShouldBe(expected);
    }

    [Fact]
    public void CaseSensitivePattern_PreservesOrdinalCase()
    {
        var pattern = new SearchPattern("File?.TXT") { CaseSensitive = true };
        pattern.IsMatch("File1.TXT").ShouldBeTrue();
        pattern.IsMatch("file1.txt").ShouldBeFalse();
        Should.Throw<ArgumentNullException>(() => new SearchPattern(null!));
    }
}
