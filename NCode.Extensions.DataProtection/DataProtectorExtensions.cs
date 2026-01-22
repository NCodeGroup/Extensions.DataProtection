#region Copyright Preamble

// Copyright @ 2024 NCode Group
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

using System.Buffers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using JetBrains.Annotations;
using Microsoft.AspNetCore.DataProtection;
using NCode.Buffers;

namespace NCode.Extensions.DataProtection;

/// <summary>
/// Provides extension methods for <see cref="IDataProtector"/> that enable span-based and buffer writer-based
/// cryptographic protection operations.
/// </summary>
/// <remarks>
/// <para>
/// This class provides high-performance extension methods that work with <see cref="ReadOnlySpan{T}"/> input
/// and <see cref="IBufferWriter{T}"/> output, reducing memory allocations compared to the standard array-based
/// methods provided by <see cref="IDataProtector"/>.
/// </para>
/// <para>
/// On .NET 11.0 and later, these methods leverage the native <c>ISpanDataProtector</c> interface when available.
/// On earlier framework versions, a fallback implementation is used that provides similar functionality with
/// additional security measures such as memory pinning and secure memory clearing.
/// </para>
/// </remarks>
[PublicAPI]
public static class DataProtectorExtensions
{
    /// <param name="dataProtector">The <see cref="IDataProtector"/> instance to extend with span-based operations.</param>
    extension(IDataProtector dataProtector)
    {
        /// <summary>
        /// Cryptographically protects a piece of plaintext data and writes the result to a buffer writer.
        /// </summary>
        /// <typeparam name="TWriter">The type of buffer writer to write the protected data to.
        /// Must implement <see cref="IBufferWriter{T}"/> and may be a ref struct.</typeparam>
        /// <param name="plaintext">The plaintext data to protect. May be empty but not null.</param>
        /// <param name="destination">The buffer writer to which the protected data will be written.
        /// Passed by reference to support ref struct buffer writers.</param>
        /// <exception cref="CryptographicException">
        /// Thrown if the protection operation fails due to an underlying cryptographic error.
        /// </exception>
        /// <remarks>
        /// <para>
        /// This method provides an optimized, streaming alternative to <see cref="IDataProtector.Protect(byte[])"/>.
        /// Rather than allocating an intermediate buffer, the protected data is written directly to the provided
        /// buffer writer, which can improve performance and reduce memory allocation pressure.
        /// </para>
        /// <para>
        /// The buffer writer is advanced by the total number of bytes written to it.
        /// </para>
        /// <para>
        /// <b>Implementation Details:</b>
        /// </para>
        /// <list type="bullet">
        /// <item>
        /// <description>On .NET 11.0+, uses the native <c>ISpanDataProtector.Protect</c> method when available.</description>
        /// </item>
        /// <item>
        /// <description>On earlier versions, creates a pinned array copy of the plaintext to prevent GC relocation
        /// during the protection operation.</description>
        /// </item>
        /// </list>
        /// <para>
        /// <b>Memory Management:</b> The fallback implementation uses <see cref="BufferFactory.CreatePinnedArray"/>
        /// to allocate a pinned buffer that is automatically disposed after use. This approach is preferred over
        /// <see cref="ArrayPool{T}"/> because the pool does not guarantee exact-size arrays, and
        /// <see cref="GCHandle"/> cannot be used reliably with pooled arrays.
        /// </para>
        /// </remarks>
        public void ProtectSpan<TWriter>(ReadOnlySpan<byte> plaintext, ref TWriter destination)
            where TWriter : IBufferWriter<byte>
#if NET9_0_OR_GREATER
            , allows ref struct
#endif
        {
#if NET11_0_OR_GREATER
        if (dataProtector is ISpanDataProtector spanProtector)
        {
            spanProtector.Protect(plaintext, ref destination);
        }
        else
#endif
            {
                FailbackProtectSpan(dataProtector, plaintext, ref destination);
            }
        }

        /// <summary>
        /// Cryptographically unprotects a piece of protected data and writes the result to a buffer writer.
        /// </summary>
        /// <typeparam name="TWriter">The type of buffer writer to write the unprotected data to.
        /// Must implement <see cref="IBufferWriter{T}"/> and may be a ref struct.</typeparam>
        /// <param name="protectedData">The protected data to unprotect. Must be data previously protected
        /// by the same <see cref="IDataProtector"/> or a compatible protector.</param>
        /// <param name="destination">The buffer writer to which the unprotected plaintext will be written.
        /// Passed by reference to support ref struct buffer writers.</param>
        /// <exception cref="CryptographicException">
        /// Thrown if the protected data is invalid, tampered with, or was protected using an incompatible
        /// <see cref="IDataProtector"/> instance.
        /// </exception>
        /// <remarks>
        /// <para>
        /// This method provides an optimized, streaming alternative to <see cref="IDataProtector.Unprotect(byte[])"/>.
        /// Rather than allocating an intermediate buffer, the unprotected plaintext is written directly to the provided
        /// buffer writer, which can improve performance and reduce memory allocation pressure.
        /// </para>
        /// <para>
        /// The buffer writer is advanced by the total number of bytes written to it.
        /// </para>
        /// <para>
        /// <b>Implementation Details:</b>
        /// </para>
        /// <list type="bullet">
        /// <item>
        /// <description>On .NET 11.0+, uses the native <c>ISpanDataProtector.Unprotect</c> method when available.</description>
        /// </item>
        /// <item>
        /// <description>On earlier versions, the plaintext is pinned in memory and securely cleared using
        /// <see cref="CryptographicOperations.ZeroMemory"/> after being written to the destination.</description>
        /// </item>
        /// </list>
        /// <para>
        /// <b>Security Note:</b> The pinning is applied as a best-effort precaution to prevent the garbage
        /// collector from relocating sensitive data after the pin is established. However, it cannot guarantee
        /// that the GC has not already moved the data between when <see cref="IDataProtector.Unprotect(byte[])"/>
        /// returns and when the pin is applied. The likelihood of this occurring is extremely low in practice,
        /// but callers with strict security requirements should be aware of this limitation. The
        /// <see cref="CryptographicOperations.ZeroMemory"/> call remains the primary security measure for
        /// clearing sensitive data from memory.
        /// </para>
        /// </remarks>
        public void UnprotectSpan<TWriter>(ReadOnlySpan<byte> protectedData, ref TWriter destination)
            where TWriter : IBufferWriter<byte>
#if NET9_0_OR_GREATER
            , allows ref struct
#endif
        {
#if NET11_0_OR_GREATER
            if (dataProtector is ISpanDataProtector spanProtector)
            {
                spanProtector.Unprotect(protectedData, ref destination);
            }
            else
#endif
            {
                FailbackUnprotectSpan(dataProtector, protectedData, ref destination);
            }
        }
    }

    internal static void FailbackProtectSpan<TWriter>(
        IDataProtector dataProtector,
        ReadOnlySpan<byte> plaintext,
        ref TWriter destination
    )
        where TWriter : IBufferWriter<byte>
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        // pin the plaintext bytes to prevent the GC from moving it around
        // can't use ArrayPool with GCHandle because it doesn't guarantee to return an exact size
        // and data protector doesn't support span (yet)
        using var plaintextBytes = BufferFactory.CreatePinnedArray(plaintext.Length);

        plaintext.CopyTo(plaintextBytes);
        var protectedBytes = dataProtector.Protect(plaintextBytes);
        var protectedLength = protectedBytes.Length;
        var destinationSpan = destination.GetSpan(protectedLength);
        protectedBytes.CopyTo(destinationSpan);
        destination.Advance(protectedLength);
    }

    internal static void FailbackUnprotectSpan<TWriter>(
        IDataProtector dataProtector,
        ReadOnlySpan<byte> protectedData,
        ref TWriter destination
    )
        where TWriter : IBufferWriter<byte>
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        var plaintextBytes = dataProtector.Unprotect(protectedData.ToArray());

        // pin the plaintextBytes quickly in order to prevent the GC from moving it around
        var plaintextHandle = GCHandle.Alloc(plaintextBytes, GCHandleType.Pinned);
        try
        {
            var plaintextLength = plaintextBytes.Length;
            var destinationSpan = destination.GetSpan(plaintextLength);
            plaintextBytes.CopyTo(destinationSpan);
            destination.Advance(plaintextLength);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintextBytes);
            plaintextHandle.Free();
        }
    }
}
