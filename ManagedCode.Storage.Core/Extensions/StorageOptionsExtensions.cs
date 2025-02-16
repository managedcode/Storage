

using System;
using System.Reflection;

namespace ManagedCode.Storage.Core.Extensions;

public static class StorageOptionsExtensions
{
    public static T DeepCopy<T>(this T? source) where T : class, IStorageOptions
    {
        if (source == null)
            return default;

        // Create new instance of the same type
        var instance = Activator.CreateInstance<T>();
        
        // Get all properties of the type
        var properties = source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            if (property.CanWrite && property.CanRead)
            {
                var value = property.GetValue(source);
                if (value != null)
                {
                    // Handle value types and strings
                    if (property.PropertyType.IsValueType || property.PropertyType == typeof(string))
                    {
                        property.SetValue(instance, value);
                    }
                    // Handle reference types by recursive deep copy
                    else
                    {
                        var deepCopyMethod = typeof(StorageOptionsExtensions)
                            .GetMethod(nameof(DeepCopy))
                            ?.MakeGenericMethod(property.PropertyType);
                            
                        if (deepCopyMethod != null)
                        {
                            var copiedValue = deepCopyMethod.Invoke(null, [value]);
                            property.SetValue(instance, copiedValue);
                        }
                    }
                }
            }
        }

        return instance;
    }
}