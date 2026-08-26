namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication;

public interface IRemoteAuthenticationServiceListener
{
    void Add(IRemoteAuthenticationServiceObserver observer);

    bool Remove(IRemoteAuthenticationServiceObserver observer);
}
