using System;
using System.IO;
using FluentAssertions;
using ManagedCode.Communication;
using ManagedCode.Storage.Core.Helpers;
using Xunit;

namespace ManagedCode.Storage.Tests.Core;

public class Crc32HelperTests
{
    [Fact]
    public void CalculateFileCrc_ShouldMatchInMemoryCalculation()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"crc-test-{Guid.NewGuid():N}.bin");
        try
        {
            var payload = new byte[4096 + 123];
            new Random(17).NextBytes(payload);
            File.WriteAllBytes(tempPath, payload);

            var fileCrc = Crc32Helper.CalculateFileCrc(tempPath);
            var inMemory = Crc32Helper.Calculate(payload);

            fileCrc.Should().Be(inMemory);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Fact]
    public void CalculateFileCrc_ForSparseGeneratedFile_ShouldBeNonZero()
    {
        using var localFile = ManagedCode.Storage.Core.Models.LocalFile.FromRandomNameWithExtension(".bin");
        ManagedCode.Storage.Tests.Common.FileHelper.GenerateLocalFile(localFile, 50);
        var crc = Crc32Helper.CalculateFileCrc(localFile.FilePath);
        crc.Should().BeGreaterThan(0U);
    }

    [Fact]
    public void ResultSucceed_ShouldCarryValue()
    {
        var result = ManagedCode.Communication.Result<uint>.Succeed(123u);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(123u);
    }

    [Fact]
    public void Calculate_ForZeroBytes_ShouldNotBeZero()
    {
        var bytes = new byte[51];
        var crc = Crc32Helper.Calculate(bytes);
        crc.Should().NotBe(0u);
    }
}
