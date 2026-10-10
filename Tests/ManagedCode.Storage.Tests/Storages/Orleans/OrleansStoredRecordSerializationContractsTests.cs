using System.Text.Json;
using System.Text.Json.Serialization;
using Orleans.Storage;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.Orleans;

public sealed class OrleansStoredRecordSerializationContractsTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RecordExists_RoundTripsWithDefaultSuppressionEnabled(bool exists)
    {
        var options = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault };
        var record = new ManagedCodeStoredGrainState<string> { State = "value", RecordExists = exists };
        JsonSerializer.Deserialize<ManagedCodeStoredGrainState<string>>(JsonSerializer.Serialize(record, options), options)!.RecordExists.ShouldBe(exists);
    }

    [Fact]
    public void LegacyPositiveRecord_WithoutAnExplicitFlagRetainsItsInterpretation()
    {
        JsonSerializer.Deserialize<ManagedCodeStoredGrainState<string>>("{\"State\":\"legacy\"}")!.RecordExists.ShouldBeTrue();
        new ManagedCodeStoredGrainState<string>().RecordExists.ShouldBeTrue();
    }
}
