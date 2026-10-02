using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace ManagedCode.Storage.Azure;

internal static class AzureMetadataTransport
{
    private const string EnvelopeKey = "managedcode_storage_metadata_v1";
    private const string EnvelopePrefix = "utf8-json-base64:";
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    internal static Dictionary<string, string>? Encode(IEnumerable<KeyValuePair<string, string>>? metadata)
    {
        if (metadata is null)
            return null;

        var values = metadata.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        if (values.All(pair => IsNativeKey(pair.Key) && IsNativeValue(pair.Value)) && !values.ContainsKey(EnvelopeKey))
            return values;

        foreach (var pair in values)
        {
            ArgumentException.ThrowIfNullOrEmpty(pair.Key);
            ArgumentNullException.ThrowIfNull(pair.Value);
            _ = StrictUtf8.GetByteCount(pair.Key);
            _ = StrictUtf8.GetByteCount(pair.Value);
        }
        return new Dictionary<string, string>
        {
            [EnvelopeKey] = EnvelopePrefix + Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(values))
        };
    }

    internal static Dictionary<string, string> Decode(IEnumerable<KeyValuePair<string, string>> metadata)
    {
        var values = metadata.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        if (!values.TryGetValue(EnvelopeKey, out var envelope) ||
            !envelope.StartsWith(EnvelopePrefix, StringComparison.Ordinal))
            return values;

        if (values.Count != 1)
            throw new InvalidDataException("Invalid Azure metadata envelope.");

        try
        {
            using var document = JsonDocument.Parse(Convert.FromBase64String(envelope[EnvelopePrefix.Length..]));
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("Invalid Azure metadata envelope.");

            var decoded = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (string.IsNullOrEmpty(property.Name) || property.Value.ValueKind != JsonValueKind.String ||
                    !decoded.TryAdd(property.Name, property.Value.GetString()!))
                    throw new InvalidDataException("Invalid Azure metadata envelope.");
            }
            return decoded;
        }
        catch (Exception exception) when (exception is FormatException or JsonException)
        {
            throw new InvalidDataException("Invalid Azure metadata envelope.", exception);
        }
    }

    private static bool IsNativeKey(string key) => key.Length > 0 && IsInitialKeyCharacter(key[0]) &&
        key.Skip(1).All(character => IsInitialKeyCharacter(character) || character is >= '0' and <= '9');

    private static bool IsInitialKeyCharacter(char character) => character is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '_';

    private static bool IsNativeValue(string value) => value is not null && value.All(character => character is >= ' ' and <= '~');
}
