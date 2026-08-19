using System.Reflection;
using Xunit;

namespace Digi21.WinUI.Ribbon.Tests;

public class SmokeTests
{
    [Fact]
    public void LibraryAssemblyLoads()
    {
        // Ensures the library assembly can be loaded by the test host without a XAML runtime. It is
        // loaded by name rather than through a type of its own because there is no public type yet:
        // the layout algorithm and the controls arrive once the API has been agreed.
        var assembly = Assembly.Load("Digi21.WinUI.Ribbon");

        Assert.Equal("Digi21.WinUI.Ribbon", assembly.GetName().Name);
    }
}
