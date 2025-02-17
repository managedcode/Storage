// using System;
// using System.Reflection;
// using System.Text.Json;
//
// namespace ManagedCode.Storage.Core.Extensions;
//
// public static class StorageOptionsExtensions
// {
//     public static T DeepCopy<T>(this T? source) where T : class, IStorageOptions
//     {
//         if (source == null)
//             return default;
//
//         var options = new JsonSerializerOptions
//         {
//             WriteIndented = false,
//             PropertyNameCaseInsensitive = true
//         };
//
//         var json = JsonSerializer.Serialize(source, source.GetType(), options);
//         return (T)JsonSerializer.Deserialize(json, source.GetType(), options)!;
//     }
// }