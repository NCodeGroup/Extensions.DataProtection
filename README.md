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

## License

Licensed under the Apache License, Version 2.0. See [LICENSE.txt](LICENSE.txt) for details.

## Release Notes
* v1.0.0 - Initial release
