using Xunit;

namespace Digi21.WinUI.Ribbon.Tests;

public class SmokeTests
{
    [Fact]
    public void LibraryAssemblyLoads()
    {
        // Ensures the library assembly can be loaded by the test host without a XAML runtime.
        var assembly = typeof(RibbonItemSize).Assembly;

        Assert.Equal("Digi21.WinUI.Ribbon", assembly.GetName().Name);
    }
}
