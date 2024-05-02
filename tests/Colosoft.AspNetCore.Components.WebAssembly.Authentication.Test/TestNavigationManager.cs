using Microsoft.AspNetCore.Components;

namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication;

internal class TestNavigationManager : NavigationManager
{
    public TestNavigationManager() =>
        this.Initialize("https://www.example.com/base/", "https://www.example.com/base/add-product");

    protected override void NavigateToCore(string uri, bool forceLoad) => throw new NotImplementedException();
}
