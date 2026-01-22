using Microsoft.AspNetCore.DataProtection;
using Moq;

namespace NCode.Extensions.DataProtection.Tests;

public class DataProtectorFactoryTests
{
    #region GetPurpose Tests

    [Fact]
    public void GetPurpose_WithRegularType_ReturnsFullTypeName()
    {
        var mockProvider = new Mock<IDataProtectionProvider>(MockBehavior.Strict);
        var factory = new DataProtectorFactory<DataProtectorFactoryTests>(mockProvider.Object);

        var result = factory.GetPurpose();

        Assert.Equal(typeof(DataProtectorFactoryTests).FullName, result);
    }

    [Fact]
    public void GetPurpose_WithNestedType_ReturnsFullTypeName()
    {
        var mockProvider = new Mock<IDataProtectionProvider>(MockBehavior.Strict);
        var factory = new DataProtectorFactory<NestedTestClass>(mockProvider.Object);

        var result = factory.GetPurpose();

        Assert.Equal(typeof(NestedTestClass).FullName, result);
    }

    [Fact]
    public void GetPurpose_WithGenericType_ReturnsFullTypeName()
    {
        var mockProvider = new Mock<IDataProtectionProvider>(MockBehavior.Strict);
        var factory = new DataProtectorFactory<List<string>>(mockProvider.Object);

        var result = factory.GetPurpose();

        Assert.Equal(typeof(List<string>).FullName, result);
    }

    [Fact]
    public void GetPurpose_CalledMultipleTimes_ReturnsSameValue()
    {
        var mockProvider = new Mock<IDataProtectionProvider>(MockBehavior.Strict);
        var factory = new DataProtectorFactory<DataProtectorFactoryTests>(mockProvider.Object);

        var result1 = factory.GetPurpose();
        var result2 = factory.GetPurpose();

        Assert.Equal(result1, result2);
    }

    #endregion

    #region CreateDataProtector Tests

    [Fact]
    public void CreateDataProtector_CallsProviderWithCorrectPurpose()
    {
        var expectedPurpose = typeof(DataProtectorFactoryTests).FullName!;
        var mockProtector = new Mock<IDataProtector>(MockBehavior.Strict);

        var mockProvider = new Mock<IDataProtectionProvider>(MockBehavior.Strict);
        mockProvider
            .Setup(x => x.CreateProtector(expectedPurpose))
            .Returns(mockProtector.Object)
            .Verifiable();

        var factory = new DataProtectorFactory<DataProtectorFactoryTests>(mockProvider.Object);

        var result = factory.CreateDataProtector();

        Assert.Same(mockProtector.Object, result);
        mockProvider.Verify();
    }

    [Fact]
    public void CreateDataProtector_WithGenericType_CallsProviderWithCorrectPurpose()
    {
        var expectedPurpose = typeof(Dictionary<string, int>).FullName!;
        var mockProtector = new Mock<IDataProtector>(MockBehavior.Strict);

        var mockProvider = new Mock<IDataProtectionProvider>(MockBehavior.Strict);
        mockProvider
            .Setup(x => x.CreateProtector(expectedPurpose))
            .Returns(mockProtector.Object)
            .Verifiable();

        var factory = new DataProtectorFactory<Dictionary<string, int>>(mockProvider.Object);

        var result = factory.CreateDataProtector();

        Assert.Same(mockProtector.Object, result);
        mockProvider.Verify();
    }

    [Fact]
    public void CreateDataProtector_CalledMultipleTimes_CallsProviderEachTime()
    {
        var expectedPurpose = typeof(DataProtectorFactoryTests).FullName!;
        var mockProtector1 = new Mock<IDataProtector>(MockBehavior.Strict);
        var mockProtector2 = new Mock<IDataProtector>(MockBehavior.Strict);

        var mockProvider = new Mock<IDataProtectionProvider>(MockBehavior.Strict);
        mockProvider
            .SetupSequence(x => x.CreateProtector(expectedPurpose))
            .Returns(mockProtector1.Object)
            .Returns(mockProtector2.Object);

        var factory = new DataProtectorFactory<DataProtectorFactoryTests>(mockProvider.Object);

        var result1 = factory.CreateDataProtector();
        var result2 = factory.CreateDataProtector();

        Assert.Same(mockProtector1.Object, result1);
        Assert.Same(mockProtector2.Object, result2);
        mockProvider.Verify(x => x.CreateProtector(expectedPurpose), Times.Exactly(2));
    }

    [Fact]
    public void CreateDataProtector_WhenProviderThrows_PropagatesException()
    {
        var expectedPurpose = typeof(DataProtectorFactoryTests).FullName!;
        var expectedException = new InvalidOperationException("Provider failed");

        var mockProvider = new Mock<IDataProtectionProvider>(MockBehavior.Strict);
        mockProvider
            .Setup(x => x.CreateProtector(expectedPurpose))
            .Throws(expectedException)
            .Verifiable();

        var factory = new DataProtectorFactory<DataProtectorFactoryTests>(mockProvider.Object);

        var actualException = Assert.Throws<InvalidOperationException>(() => factory.CreateDataProtector());

        Assert.Same(expectedException, actualException);
        mockProvider.Verify();
    }

    #endregion

    #region Custom GetPurpose Tests

    [Fact]
    public void CreateDataProtector_WithCustomGetPurpose_UsesCustomPurpose()
    {
        const string customPurpose = "MyCustomPurpose";
        var mockProtector = new Mock<IDataProtector>(MockBehavior.Strict);

        var mockProvider = new Mock<IDataProtectionProvider>(MockBehavior.Strict);
        mockProvider
            .Setup(x => x.CreateProtector(customPurpose))
            .Returns(mockProtector.Object)
            .Verifiable();

        var factory = new CustomPurposeFactory<DataProtectorFactoryTests>(mockProvider.Object, customPurpose);

        var result = factory.CreateDataProtector();

        Assert.Same(mockProtector.Object, result);
        mockProvider.Verify();
    }

    #endregion

    #region Test Helpers

    private class NestedTestClass;

    private class CustomPurposeFactory<T>(
        IDataProtectionProvider dataProtectionProvider,
        string customPurpose
    ) : DataProtectorFactory<T>(dataProtectionProvider)
    {
        public override string GetPurpose() => customPurpose;
    }

    #endregion
}
