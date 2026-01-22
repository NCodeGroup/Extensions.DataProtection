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

using JetBrains.Annotations;
using Microsoft.AspNetCore.DataProtection;

namespace NCode.Extensions.DataProtection;

/// <summary>
/// Defines a factory for creating <see cref="IDataProtector"/> instances with a purpose string
/// derived from the type parameter <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type used to derive the purpose string for the data protector.
/// This ensures cryptographic isolation between different components or features.</typeparam>
/// <remarks>
/// <para>
/// This factory pattern provides a type-safe way to obtain <see cref="IDataProtector"/> instances
/// where each type <typeparamref name="T"/> results in a unique purpose string, ensuring that
/// data protected by one component cannot be unprotected by another.
/// </para>
/// <para>
/// Register this service using the extension methods in <see cref="Registration"/>.
/// </para>
/// </remarks>
[PublicAPI]
public interface IDataProtectorFactory<T>
{
    /// <summary>
    /// Creates a new <see cref="IDataProtector"/> instance with a purpose string derived from
    /// the type parameter <typeparamref name="T"/>.
    /// </summary>
    /// <returns>An <see cref="IDataProtector"/> configured with the appropriate purpose string.</returns>
    IDataProtector CreateDataProtector();
}

/// <summary>
/// Default implementation of <see cref="IDataProtectorFactory{T}"/> that creates data protectors
/// using the full type name of <typeparamref name="T"/> as the purpose string.
/// </summary>
/// <typeparam name="T">The type used to derive the purpose string for the data protector.</typeparam>
/// <param name="dataProtectionProvider">The underlying <see cref="IDataProtectionProvider"/>
/// used to create <see cref="IDataProtector"/> instances.</param>
/// <remarks>
/// <para>
/// This implementation uses <see cref="Type.FullName"/> (or <see cref="System.Reflection.MemberInfo.Name"/> as fallback)
/// of <typeparamref name="T"/> as the purpose string, providing automatic cryptographic isolation
/// based on type identity.
/// </para>
/// <para>
/// The <see cref="GetPurpose"/> method is virtual, allowing derived classes to customize
/// the purpose string generation logic if needed.
/// </para>
/// </remarks>
[PublicAPI]
public class DataProtectorFactory<T>(
    IDataProtectionProvider dataProtectionProvider
) : IDataProtectorFactory<T>
{
    private IDataProtectionProvider DataProtectionProvider { get; } = dataProtectionProvider;

    /// <summary>
    /// Gets the purpose string used to create the <see cref="IDataProtector"/>.
    /// </summary>
    /// <returns>The full type name of <typeparamref name="T"/>, or the simple type name if the full name is unavailable.</returns>
    /// <remarks>
    /// Override this method in a derived class to customize the purpose string generation.
    /// </remarks>
    public virtual string GetPurpose() => typeof(T).FullName ?? typeof(T).Name;

    /// <inheritdoc />
    public IDataProtector CreateDataProtector()
    {
        return DataProtectionProvider.CreateProtector(GetPurpose());
    }
}
