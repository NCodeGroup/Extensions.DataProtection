[![ci](https://github.com/NCodeGroup/Extensions.DataProtection/actions/workflows/main.yml/badge.svg)](https://github.com/NCodeGroup/Extensions.DataProtection/actions)
[![Nuget](https://img.shields.io/nuget/v/NCode.Extensions.DataProtection.svg)](https://www.nuget.org/packages/NCode.Extensions.DataProtection/)

# NCode.Extensions.DataProtection

A .NET library that provides extensions and utilities for ASP.NET Core Data Protection, including a type-safe factory pattern for creating `IDataProtector` instances and high-performance span-based protection operations.

## Features

- **Type-Safe Data Protector Factory** (`IDataProtectorFactory<T>`)
  - Creates `IDataProtector` instances with purpose strings derived from generic type parameters
  - Ensures cryptographic isolation between different components automatically
  - Supports custom factory implementations for advanced scenarios

- **High-Performance Span Extensions** (`DataProtectorExtensions`)
  - `ProtectSpan<TWriter>` - Protects plaintext data using `ReadOnlySpan<byte>` input and `IBufferWriter<byte>` output
  - `UnprotectSpan<TWriter>` - Unprotects data using `ReadOnlySpan<byte>` input and `IBufferWriter<byte>` output
  - Reduces memory allocations compared to standard array-based methods
  - Leverages native `ISpanDataProtector` on .NET 11.0+ when available
  - Includes security measures like memory pinning and secure memory clearing on older frameworks

- **Dependency Injection Integration**
  - `AddDataProtectorFactory()` - Registers the open generic factory for any `IDataProtectorFactory<T>`
  - `AddDataProtectorFactory<T, TImplementation>()` - Registers a custom factory implementation for a specific type
  - Uses `TryAdd` semantics to avoid overwriting existing registrations

## Installation

```shell
dotnet add package NCode.Extensions.DataProtection
```

## Usage

### Basic Setup

```csharp
// Configure Data Protection (your responsibility to set up the keyring)
services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"c:\keys"))
    .ProtectKeysWithDpapi();

// Register the data protector factory
services.AddDataProtectorFactory();
```

### Using the Factory

```csharp
public class MyService
{
    private readonly IDataProtector _protector;

    public MyService(IDataProtectorFactory<MyService> factory)
    {
        _protector = factory.CreateDataProtector();
    }

    public string Protect(string data) => _protector.Protect(data);
    public string Unprotect(string data) => _protector.Unprotect(data);
}
```

### Using Span Extensions

```csharp
var protector = factory.CreateDataProtector();
var writer = new ArrayBufferWriter<byte>();

// Protect data
ReadOnlySpan<byte> plaintext = "sensitive data"u8;
protector.ProtectSpan(plaintext, ref writer);

// Unprotect data
var unprotectWriter = new ArrayBufferWriter<byte>();
protector.UnprotectSpan(writer.WrittenSpan, ref unprotectWriter);
```

## Requirements

- .NET 8.0, .NET 9.0, or .NET 10.0+
- Microsoft.AspNetCore.DataProtection

## Known Limitations

### UnprotectSpan Memory Pinning (Pre-.NET 11.0)

> ⚠️ **Security Notice**: On frameworks prior to .NET 11.0, `UnprotectSpan` cannot guarantee that sensitive plaintext data won't be copied by the garbage collector before memory pinning is applied.

When using `UnprotectSpan` on .NET 8.0, 9.0, or 10.0, the implementation:

1. Calls `IDataProtector.Unprotect()` which returns a `byte[]` containing the plaintext
2. Immediately attempts to pin the array using `GCHandle.Alloc(..., GCHandleType.Pinned)`
3. Copies the data to the destination buffer
4. Clears the memory using `CryptographicOperations.ZeroMemory()`

**The limitation**: Between steps 1 and 2, there is a brief window where the GC could relocate the plaintext array, potentially leaving copies of sensitive data in memory that cannot be cleared.

While the likelihood of this occurring is extremely low in practice, applications with strict security requirements should be aware of this limitation. The `ZeroMemory` call remains the primary security measure for clearing sensitive data.

**On .NET 11.0+**, the native `ISpanDataProtector` interface is used when available, which eliminates this limitation entirely.

## License

Licensed under the Apache License, Version 2.0. See [LICENSE.txt](LICENSE.txt) for details.

## Release Notes
* v1.0.0 - Initial release
* v1.0.1 - Fix xmldoc
