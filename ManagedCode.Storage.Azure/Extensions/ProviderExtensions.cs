using System;
using Microsoft.Extensions.DependencyInjection;
using Azure.Storage.Blobs;
using ManagedCode.Storage.Core.Builders;
using System.Reflection.Emit;
using System.Reflection;

namespace ManagedCode.Storage.Azure.Extensions
{
    public static class ProviderExtensions
    {
        public static ProviderBuilder AddAzureBlobStorage<TAzureStorage>(
            this ProviderBuilder providerBuilder, 
            Action<AzureBlobStorageConnectionOptions> action)
            where TAzureStorage : IAzureBlobStorage
        {
            var connectionOptions = new AzureBlobStorageConnectionOptions();
            action.Invoke(connectionOptions);

            var typeSignature = typeof(TAzureStorage).Name;
            var an = new AssemblyName(typeSignature);
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
            TypeBuilder tb = moduleBuilder.DefineType(typeSignature,
                    TypeAttributes.Public |
                    TypeAttributes.Class |
                    TypeAttributes.AutoClass |
                    TypeAttributes.AnsiClass |
                    TypeAttributes.BeforeFieldInit |
                    TypeAttributes.AutoLayout,
                    null);

            tb.SetParent(typeof(AzureBlobStorage));
            tb.AddInterfaceImplementation(typeof(TAzureStorage));

            ConstructorBuilder constructor = tb.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
            Type implType = tb.CreateType();

            providerBuilder.ServiceCollection.AddScoped(typeof(TAzureStorage), implType);
            
            return providerBuilder;
        }
    }
}
