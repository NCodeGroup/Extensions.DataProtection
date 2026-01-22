#region Copyright Preamble

// Copyright @ 2026 NCode Group
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

#endregion

using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace NCode.Extensions.DataProtection;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/> to register data protector factory services.
/// </summary>
/// <remarks>
/// <para>
/// This class provides methods to register <see cref="IDataProtectorFactory{T}"/> implementations in the
/// dependency injection container. The factory pattern allows for type-safe, purpose-specific
/// <see cref="Microsoft.AspNetCore.DataProtection.IDataProtector"/> instances to be created.
/// </para>
/// <para>
/// The generic type parameter on the factory interface is used to derive the purpose string for the
/// data protector, ensuring that different components have isolated cryptographic protection.
/// </para>
/// </remarks>
[PublicAPI]
[ExcludeFromCodeCoverage]
public static class Registration
{
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the open generic <see cref="DataProtectorFactory{T}"/> as the default implementation
        /// of <see cref="IDataProtectorFactory{T}"/> in the service collection.
        /// </summary>
        /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers an open generic singleton service that will be resolved for any
        /// closed generic type of <see cref="IDataProtectorFactory{T}"/>.
        /// </para>
        /// <para>
        /// The registration uses <see cref="ServiceCollectionDescriptorExtensions.TryAdd(IServiceCollection, ServiceDescriptor)"/> semantics,
        /// meaning it will not overwrite any existing registration for the same service type.
        /// </para>
        /// <para>
        /// This method does not call <c>AddDataProtection</c> because it is the consumer's responsibility
        /// to configure the Data Protection keyring according to their application's requirements.
        /// </para>
        /// <para>
        /// <b>Example usage:</b>
        /// </para>
        /// <code>
        /// services.AddDataProtection();
        /// services.AddDataProtectorFactory();
        /// </code>
        /// </remarks>
        public IServiceCollection AddDataProtectorFactory()
        {
            services.TryAdd(
                ServiceDescriptor.Singleton(typeof(IDataProtectorFactory<>), typeof(DataProtectorFactory<>))
            );
            return services;
        }

        /// <summary>
        /// Registers a custom <see cref="IDataProtectorFactory{T}"/> implementation for a specific type.
        /// </summary>
        /// <typeparam name="T">The type used as the key for the data protector factory.
        /// This type's full name is typically used to derive the protection purpose string.</typeparam>
        /// <typeparam name="TImplementation">The custom implementation type of <see cref="IDataProtectorFactory{T}"/>
        /// that provides customized data protector creation logic.</typeparam>
        /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
        /// <remarks>
        /// <para>
        /// Use this overload when you need a custom implementation of <see cref="IDataProtectorFactory{T}"/>
        /// for a specific type, for example to customize the purpose string or use a different
        /// <see cref="Microsoft.AspNetCore.DataProtection.IDataProtectionProvider"/>.
        /// </para>
        /// <para>
        /// The registration uses <see cref="ServiceCollectionDescriptorExtensions.TryAddSingleton{TService,TImplementation}(IServiceCollection)"/>
        /// semantics, meaning it will not overwrite any existing registration for the same service type.
        /// </para>
        /// <para>
        /// <b>Example usage:</b>
        /// </para>
        /// <code>
        /// public class CustomDataProtectorFactory : IDataProtectorFactory&lt;MySecrets&gt;
        /// {
        ///     public IDataProtector CreateDataProtector() =&gt;
        ///         _provider.CreateProtector("MyApplication.Secrets.v1");
        /// }
        ///
        /// services.AddDataProtectorFactory&lt;MySecrets, CustomDataProtectorFactory&gt;();
        /// </code>
        /// </remarks>
        public IServiceCollection AddDataProtectorFactory<T,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>()
            where T : class
            where TImplementation : class, IDataProtectorFactory<T>
        {
            services.TryAddSingleton<IDataProtectorFactory<T>, TImplementation>();
            return services;
        }
    }
}
