using System.Buffers;
using Microsoft.AspNetCore.DataProtection;
using Moq;

namespace NCode.Extensions.DataProtection.Tests;

public class DataProtectorExtensionsTests
{
    #region FailbackProtectSpan Tests

    [Fact]
    public void FailbackProtectSpan_WithValidPlaintext_WritesProtectedDataToDestination()
    {
        byte[] plaintext = "Hello, World!"u8.ToArray();
        byte[] expectedProtectedData = [0x01, 0x02, 0x03, 0x04, 0x05];

        var mockDataProtector = new Mock<IDataProtector>(MockBehavior.Strict);
        mockDataProtector
            .Setup(x => x.Protect(It.Is<byte[]>(b => b.SequenceEqual(plaintext))))
            .Returns(expectedProtectedData)
            .Verifiable();

        var bufferWriter = new ArrayBufferWriter<byte>();

        DataProtectorExtensions.FailbackProtectSpan(mockDataProtector.Object, plaintext, ref bufferWriter);

        Assert.Equal(expectedProtectedData.Length, bufferWriter.WrittenCount);
        Assert.Equal(expectedProtectedData, bufferWriter.WrittenSpan.ToArray());
        mockDataProtector.Verify();
    }

    [Fact]
    public void FailbackProtectSpan_WithEmptyPlaintext_WritesProtectedDataToDestination()
    {
        byte[] plaintext = [];
        byte[] expectedProtectedData = [0xAA, 0xBB, 0xCC];

        var mockDataProtector = new Mock<IDataProtector>(MockBehavior.Strict);
        mockDataProtector
            .Setup(x => x.Protect(It.Is<byte[]>(b => b.Length == 0)))
            .Returns(expectedProtectedData)
            .Verifiable();

        var bufferWriter = new ArrayBufferWriter<byte>();

        DataProtectorExtensions.FailbackProtectSpan(mockDataProtector.Object, plaintext, ref bufferWriter);

        Assert.Equal(expectedProtectedData.Length, bufferWriter.WrittenCount);
        Assert.Equal(expectedProtectedData, bufferWriter.WrittenSpan.ToArray());
        mockDataProtector.Verify();
    }

    [Fact]
    public void FailbackProtectSpan_WithLargePlaintext_WritesProtectedDataToDestination()
    {
        var plaintext = new byte[4096];
        Random.Shared.NextBytes(plaintext);

        var expectedProtectedData = new byte[4200];
        Random.Shared.NextBytes(expectedProtectedData);

        var mockDataProtector = new Mock<IDataProtector>(MockBehavior.Strict);
        mockDataProtector
            .Setup(x => x.Protect(It.Is<byte[]>(b => b.SequenceEqual(plaintext))))
            .Returns(expectedProtectedData)
            .Verifiable();

        var bufferWriter = new ArrayBufferWriter<byte>();

        DataProtectorExtensions.FailbackProtectSpan(mockDataProtector.Object, plaintext, ref bufferWriter);

        Assert.Equal(expectedProtectedData.Length, bufferWriter.WrittenCount);
        Assert.Equal(expectedProtectedData, bufferWriter.WrittenSpan.ToArray());
        mockDataProtector.Verify();
    }

    [Fact]
    public void FailbackProtectSpan_CopiesPlaintextToTemporaryBuffer()
    {
        byte[] plaintext = [0x10, 0x20, 0x30, 0x40];
        byte[] protectedData = [0xDE, 0xAD, 0xBE, 0xEF];
        byte[]? capturedInput = null;

        var mockDataProtector = new Mock<IDataProtector>(MockBehavior.Strict);
        mockDataProtector
            .Setup(x => x.Protect(It.IsAny<byte[]>()))
            .Callback<byte[]>(input => capturedInput = input.ToArray())
            .Returns(protectedData)
            .Verifiable();

        var bufferWriter = new ArrayBufferWriter<byte>();

        DataProtectorExtensions.FailbackProtectSpan(mockDataProtector.Object, plaintext, ref bufferWriter);

        Assert.NotNull(capturedInput);
        Assert.Equal(plaintext, capturedInput);
        mockDataProtector.Verify();
    }

    [Fact]
    public void FailbackProtectSpan_AdvancesBufferWriterByProtectedDataLength()
    {
        byte[] plaintext = [0x01, 0x02];
        byte[] protectedData = [0x10, 0x20, 0x30, 0x40, 0x50];

        var mockDataProtector = new Mock<IDataProtector>(MockBehavior.Strict);
        mockDataProtector
            .Setup(x => x.Protect(It.IsAny<byte[]>()))
            .Returns(protectedData)
            .Verifiable();

        var bufferWriter = new ArrayBufferWriter<byte>();

        DataProtectorExtensions.FailbackProtectSpan(mockDataProtector.Object, plaintext, ref bufferWriter);

        Assert.Equal(protectedData.Length, bufferWriter.WrittenCount);
        mockDataProtector.Verify();
    }

    [Fact]
    public void FailbackProtectSpan_WithReadOnlySpanFromArray_ProtectsCorrectly()
    {
        byte[] sourceArray = [0x00, 0x11, 0x22, 0x33, 0x44, 0x55];
        ReadOnlySpan<byte> plaintext = sourceArray.AsSpan(1, 4);
        byte[] expectedPlaintext = [0x11, 0x22, 0x33, 0x44];
        byte[] protectedData = [0xAB, 0xCD, 0xEF];

        var mockDataProtector = new Mock<IDataProtector>(MockBehavior.Strict);
        mockDataProtector
            .Setup(x => x.Protect(It.Is<byte[]>(b => b.SequenceEqual(expectedPlaintext))))
            .Returns(protectedData)
            .Verifiable();

        var bufferWriter = new ArrayBufferWriter<byte>();

        DataProtectorExtensions.FailbackProtectSpan(mockDataProtector.Object, plaintext, ref bufferWriter);

        Assert.Equal(protectedData.Length, bufferWriter.WrittenCount);
        Assert.Equal(protectedData, bufferWriter.WrittenSpan.ToArray());
        mockDataProtector.Verify();
    }

    [Fact]
    public void FailbackProtectSpan_WithPreExistingDataInWriter_AppendsProtectedData()
    {
        byte[] plaintext = [0x01, 0x02, 0x03];
        byte[] protectedData = [0xAA, 0xBB];
        byte[] preExistingData = [0xFF, 0xEE, 0xDD];

        var mockDataProtector = new Mock<IDataProtector>(MockBehavior.Strict);
        mockDataProtector
            .Setup(x => x.Protect(It.IsAny<byte[]>()))
            .Returns(protectedData)
            .Verifiable();

        var bufferWriter = new ArrayBufferWriter<byte>();
        bufferWriter.Write(preExistingData);

        DataProtectorExtensions.FailbackProtectSpan(mockDataProtector.Object, plaintext, ref bufferWriter);

        Assert.Equal(preExistingData.Length + protectedData.Length, bufferWriter.WrittenCount);
        Assert.Equal(preExistingData, bufferWriter.WrittenSpan[..preExistingData.Length].ToArray());
        Assert.Equal(protectedData, bufferWriter.WrittenSpan[preExistingData.Length..].ToArray());
        mockDataProtector.Verify();
    }

    [Fact]
    public void FailbackProtectSpan_WhenProtectThrows_PropagatesException()
    {
        byte[] plaintext = [0x01, 0x02, 0x03];
        var expectedException = new InvalidOperationException("Protection failed");

        var mockDataProtector = new Mock<IDataProtector>(MockBehavior.Strict);
        mockDataProtector
            .Setup(x => x.Protect(It.IsAny<byte[]>()))
            .Throws(expectedException)
            .Verifiable();

        var bufferWriter = new ArrayBufferWriter<byte>();

        var actualException = Assert.Throws<InvalidOperationException>(() =>
            DataProtectorExtensions.FailbackProtectSpan(mockDataProtector.Object, plaintext, ref bufferWriter));

        Assert.Same(expectedException, actualException);
        mockDataProtector.Verify();
    }

    [Fact]
    public void FailbackProtectSpan_WithSingleBytePlaintext_WritesProtectedDataToDestination()
    {
        byte[] plaintext = [0x42];
        byte[] protectedData = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];

        var mockDataProtector = new Mock<IDataProtector>(MockBehavior.Strict);
        mockDataProtector
            .Setup(x => x.Protect(It.Is<byte[]>(b => b.Length == 1 && b[0] == 0x42)))
            .Returns(protectedData)
            .Verifiable();

        var bufferWriter = new ArrayBufferWriter<byte>();

        DataProtectorExtensions.FailbackProtectSpan(mockDataProtector.Object, plaintext, ref bufferWriter);

        Assert.Equal(protectedData.Length, bufferWriter.WrittenCount);
        Assert.Equal(protectedData, bufferWriter.WrittenSpan.ToArray());
        mockDataProtector.Verify();
    }

    [Fact]
    public void FailbackProtectSpan_CalledMultipleTimes_AccumulatesInWriter()
    {
        byte[] plaintext1 = [0x01, 0x02];
        byte[] plaintext2 = [0x03, 0x04];
        byte[] protectedData1 = [0xAA, 0xBB, 0xCC];
        byte[] protectedData2 = [0xDD, 0xEE];

        var mockDataProtector = new Mock<IDataProtector>(MockBehavior.Strict);
        mockDataProtector
            .Setup(x => x.Protect(It.Is<byte[]>(b => b.SequenceEqual(plaintext1))))
            .Returns(protectedData1)
            .Verifiable();
        mockDataProtector
            .Setup(x => x.Protect(It.Is<byte[]>(b => b.SequenceEqual(plaintext2))))
            .Returns(protectedData2)
            .Verifiable();

        var bufferWriter = new ArrayBufferWriter<byte>();

        DataProtectorExtensions.FailbackProtectSpan(mockDataProtector.Object, plaintext1, ref bufferWriter);
        DataProtectorExtensions.FailbackProtectSpan(mockDataProtector.Object, plaintext2, ref bufferWriter);

        Assert.Equal(protectedData1.Length + protectedData2.Length, bufferWriter.WrittenCount);
        Assert.Equal(protectedData1, bufferWriter.WrittenSpan[..protectedData1.Length].ToArray());
        Assert.Equal(protectedData2, bufferWriter.WrittenSpan[protectedData1.Length..].ToArray());
        mockDataProtector.Verify();
    }

    #endregion

    #region FailbackUnprotectSpan Tests

    [Fact]
    public void FailbackUnprotectSpan_WithValidProtectedData_WritesPlaintextToDestination()
    {
        byte[] protectedData = [0x01, 0x02, 0x03, 0x04, 0x05];
        byte[] expectedPlaintext = "Hello, World!"u8.ToArray();

        var mockDataProtector = new Mock<IDataProtector>(MockBehavior.Strict);
        mockDataProtector
            .Setup(x => x.Unprotect(It.Is<byte[]>(b => b.SequenceEqual(protectedData))))
            .Returns(() => expectedPlaintext.ToArray())
            .Verifiable();

        var bufferWriter = new ArrayBufferWriter<byte>();

        DataProtectorExtensions.FailbackUnprotectSpan(mockDataProtector.Object, protectedData, ref bufferWriter);

        Assert.Equal(expectedPlaintext.Length, bufferWriter.WrittenCount);
        Assert.Equal(expectedPlaintext, bufferWriter.WrittenSpan.ToArray());
        mockDataProtector.Verify();
    }

    [Fact]
    public void FailbackUnprotectSpan_WithEmptyProtectedData_WritesPlaintextToDestination()
    {
        byte[] protectedData = [0xAA, 0xBB, 0xCC];
        byte[] expectedPlaintext = [];

        var mockDataProtector = new Mock<IDataProtector>(MockBehavior.Strict);
        mockDataProtector
            .Setup(x => x.Unprotect(It.Is<byte[]>(b => b.SequenceEqual(protectedData))))
            .Returns(expectedPlaintext)
            .Verifiable();

        var bufferWriter = new ArrayBufferWriter<byte>();

        DataProtectorExtensions.FailbackUnprotectSpan(mockDataProtector.Object, protectedData, ref bufferWriter);

        Assert.Empty(bufferWriter.WrittenSpan.ToArray());
        mockDataProtector.Verify();
    }

    [Fact]
    public void FailbackUnprotectSpan_WithLargeProtectedData_WritesPlaintextToDestination()
    {
        var protectedData = new byte[4200];
        Random.Shared.NextBytes(protectedData);

        var expectedPlaintext = new byte[4096];
        Random.Shared.NextBytes(expectedPlaintext);

        var mockDataProtector = new Mock<IDataProtector>(MockBehavior.Strict);
        mockDataProtector
            .Setup(x => x.Unprotect(It.Is<byte[]>(b => b.SequenceEqual(protectedData))))
            .Returns(() => expectedPlaintext.ToArray())
            .Verifiable();

        var bufferWriter = new ArrayBufferWriter<byte>();

        DataProtectorExtensions.FailbackUnprotectSpan(mockDataProtector.Object, protectedData, ref bufferWriter);

        Assert.Equal(expectedPlaintext.Length, bufferWriter.WrittenCount);
        Assert.Equal(expectedPlaintext, bufferWriter.WrittenSpan.ToArray());
        mockDataProtector.Verify();
    }

    [Fact]
    public void FailbackUnprotectSpan_CopiesProtectedDataToArray()
    {
        byte[] protectedData = [0x10, 0x20, 0x30, 0x40];
        byte[] plaintext = [0xDE, 0xAD, 0xBE, 0xEF];
        byte[]? capturedInput = null;

        var mockDataProtector = new Mock<IDataProtector>(MockBehavior.Strict);
        mockDataProtector
            .Setup(x => x.Unprotect(It.IsAny<byte[]>()))
            .Callback<byte[]>(input => capturedInput = input.ToArray())
            .Returns(() => plaintext.ToArray())
            .Verifiable();

        var bufferWriter = new ArrayBufferWriter<byte>();

        DataProtectorExtensions.FailbackUnprotectSpan(mockDataProtector.Object, protectedData, ref bufferWriter);

        Assert.NotNull(capturedInput);
        Assert.Equal(protectedData, capturedInput);
        mockDataProtector.Verify();
    }

    [Fact]
    public void FailbackUnprotectSpan_AdvancesBufferWriterByPlaintextLength()
    {
        byte[] protectedData = [0x10, 0x20, 0x30, 0x40, 0x50];
        byte[] plaintext = [0x01, 0x02];

        var mockDataProtector = new Mock<IDataProtector>(MockBehavior.Strict);
        mockDataProtector
            .Setup(x => x.Unprotect(It.IsAny<byte[]>()))
            .Returns(() => plaintext.ToArray())
            .Verifiable();

        var bufferWriter = new ArrayBufferWriter<byte>();

        DataProtectorExtensions.FailbackUnprotectSpan(mockDataProtector.Object, protectedData, ref bufferWriter);

        Assert.Equal(plaintext.Length, bufferWriter.WrittenCount);
        mockDataProtector.Verify();
    }

    [Fact]
    public void FailbackUnprotectSpan_WithReadOnlySpanFromArray_UnprotectsCorrectly()
    {
        byte[] sourceArray = [0x00, 0x11, 0x22, 0x33, 0x44, 0x55];
        ReadOnlySpan<byte> protectedData = sourceArray.AsSpan(1, 4);
        byte[] expectedProtectedData = [0x11, 0x22, 0x33, 0x44];
        byte[] plaintext = [0xAB, 0xCD, 0xEF];

        var mockDataProtector = new Mock<IDataProtector>(MockBehavior.Strict);
        mockDataProtector
            .Setup(x => x.Unprotect(It.Is<byte[]>(b => b.SequenceEqual(expectedProtectedData))))
            .Returns(() => plaintext.ToArray())
            .Verifiable();

        var bufferWriter = new ArrayBufferWriter<byte>();

        DataProtectorExtensions.FailbackUnprotectSpan(mockDataProtector.Object, protectedData, ref bufferWriter);

        Assert.Equal(plaintext.Length, bufferWriter.WrittenCount);
        Assert.Equal(plaintext, bufferWriter.WrittenSpan.ToArray());
        mockDataProtector.Verify();
    }

    [Fact]
    public void FailbackUnprotectSpan_WithPreExistingDataInWriter_AppendsPlaintext()
    {
        byte[] protectedData = [0x01, 0x02, 0x03];
        byte[] plaintext = [0xAA, 0xBB];
        byte[] preExistingData = [0xFF, 0xEE, 0xDD];

        var mockDataProtector = new Mock<IDataProtector>(MockBehavior.Strict);
        mockDataProtector
            .Setup(x => x.Unprotect(It.IsAny<byte[]>()))
            .Returns(() => plaintext.ToArray())
            .Verifiable();

        var bufferWriter = new ArrayBufferWriter<byte>();
        bufferWriter.Write(preExistingData);

        DataProtectorExtensions.FailbackUnprotectSpan(mockDataProtector.Object, protectedData, ref bufferWriter);

        Assert.Equal(preExistingData.Length + plaintext.Length, bufferWriter.WrittenCount);
        Assert.Equal(preExistingData, bufferWriter.WrittenSpan[..preExistingData.Length].ToArray());
        Assert.Equal(plaintext, bufferWriter.WrittenSpan[preExistingData.Length..].ToArray());
        mockDataProtector.Verify();
    }

    [Fact]
    public void FailbackUnprotectSpan_WhenUnprotectThrows_PropagatesException()
    {
        byte[] protectedData = [0x01, 0x02, 0x03];
        var expectedException = new InvalidOperationException("Unprotection failed");

        var mockDataProtector = new Mock<IDataProtector>(MockBehavior.Strict);
        mockDataProtector
            .Setup(x => x.Unprotect(It.IsAny<byte[]>()))
            .Throws(expectedException)
            .Verifiable();

        var bufferWriter = new ArrayBufferWriter<byte>();

        var actualException = Assert.Throws<InvalidOperationException>(() =>
            DataProtectorExtensions.FailbackUnprotectSpan(mockDataProtector.Object, protectedData, ref bufferWriter));

        Assert.Same(expectedException, actualException);
        mockDataProtector.Verify();
    }

    [Fact]
    public void FailbackUnprotectSpan_WithSingleByteProtectedData_WritesPlaintextToDestination()
    {
        byte[] protectedData = [0x42];
        byte[] plaintext = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];

        var mockDataProtector = new Mock<IDataProtector>(MockBehavior.Strict);
        mockDataProtector
            .Setup(x => x.Unprotect(It.Is<byte[]>(b => b.Length == 1 && b[0] == 0x42)))
            .Returns(() => plaintext.ToArray())
            .Verifiable();

        var bufferWriter = new ArrayBufferWriter<byte>();

        DataProtectorExtensions.FailbackUnprotectSpan(mockDataProtector.Object, protectedData, ref bufferWriter);

        Assert.Equal(plaintext.Length, bufferWriter.WrittenCount);
        Assert.Equal(plaintext, bufferWriter.WrittenSpan.ToArray());
        mockDataProtector.Verify();
    }

    [Fact]
    public void FailbackUnprotectSpan_CalledMultipleTimes_AccumulatesInWriter()
    {
        byte[] protectedData1 = [0x01, 0x02];
        byte[] protectedData2 = [0x03, 0x04];
        byte[] plaintext1 = [0xAA, 0xBB, 0xCC];
        byte[] plaintext2 = [0xDD, 0xEE];

        var mockDataProtector = new Mock<IDataProtector>(MockBehavior.Strict);
        mockDataProtector
            .Setup(x => x.Unprotect(It.Is<byte[]>(b => b.SequenceEqual(protectedData1))))
            .Returns(() => plaintext1.ToArray())
            .Verifiable();
        mockDataProtector
            .Setup(x => x.Unprotect(It.Is<byte[]>(b => b.SequenceEqual(protectedData2))))
            .Returns(() => plaintext2.ToArray())
            .Verifiable();

        var bufferWriter = new ArrayBufferWriter<byte>();

        DataProtectorExtensions.FailbackUnprotectSpan(mockDataProtector.Object, protectedData1, ref bufferWriter);
        DataProtectorExtensions.FailbackUnprotectSpan(mockDataProtector.Object, protectedData2, ref bufferWriter);

        Assert.Equal(plaintext1.Length + plaintext2.Length, bufferWriter.WrittenCount);
        Assert.Equal(plaintext1, bufferWriter.WrittenSpan[..plaintext1.Length].ToArray());
        Assert.Equal(plaintext2, bufferWriter.WrittenSpan[plaintext1.Length..].ToArray());
        mockDataProtector.Verify();
    }

    #endregion
}
