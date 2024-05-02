using Colosoft.AspNetCore.Components.WebAssembly.Authentication.Internal;
using Colosoft.AspNetCore.Components.WebAssembly.Authentication.Test.Logging;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Moq;
using System.Runtime.ExceptionServices;
using System.Security.Claims;

#pragma warning disable BL0005 // Component parameter should not be set outside of its component.
namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication;

public class RemoteAuthenticatorCoreTests
{
    private const string Action = nameof(RemoteAuthenticatorViewCore<RemoteAuthenticationState>.Action);
    private const string OnLogInSucceded = nameof(RemoteAuthenticatorViewCore<RemoteAuthenticationState>.OnLogInSucceeded);
    private const string OnLogOutSucceeded = nameof(RemoteAuthenticatorViewCore<RemoteAuthenticationState>.OnLogOutSucceeded);

    [Fact]
    public async Task AuthenticationManager_Throws_ForInvalidAction()
    {
        // Arrange
        var remoteAuthenticator = new RemoteAuthenticatorViewCore<RemoteAuthenticationState>();

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [Action] = string.Empty,
        });

        // Act & assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => remoteAuthenticator.SetParametersAsync(parameters));
    }

    [Fact]
    public async Task AuthenticationManager_Login_NavigatesToReturnUrlOnSuccess()
    {
        // Arrange
        var (remoteAuthenticator, renderer, authServiceMock) = CreateAuthenticationManager(
            "https://www.example.com/base/authentication/login",
            new InteractiveRequestOptions { Interaction = InteractionType.SignIn, ReturnUrl = "https://www.example.com/base/fetchData" }.ToState());

        authServiceMock.SignInCallback = _ => Task.FromResult(new RemoteAuthenticationResult<RemoteAuthenticationState>()
        {
            Status = RemoteAuthenticationStatus.Success,
            State = remoteAuthenticator.AuthenticationState,
        });

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [Action] = RemoteAuthenticationActions.LogIn,
        });

        // Act
        await renderer.Dispatcher.InvokeAsync<object>(() => remoteAuthenticator.SetParametersAsync(parameters));

        // Assert
        Assert.Equal("https://www.example.com/base/fetchData", remoteAuthenticator.Navigation.Uri);
    }

    [Fact]
    public async Task AuthenticationManager_Login_DoesNothingOnRedirect()
    {
        // Arrange
        var originalUrl = "https://www.example.com/base/authentication/login";
        var (remoteAuthenticator, renderer, authServiceMock) = CreateAuthenticationManager(
            originalUrl,
            new InteractiveRequestOptions { Interaction = InteractionType.SignIn, ReturnUrl = "https://www.example.com/base/fetchData" }.ToState());

        authServiceMock.SignInCallback = s => Task.FromResult(new RemoteAuthenticationResult<RemoteAuthenticationState>()
        {
            Status = RemoteAuthenticationStatus.Redirect,
            State = remoteAuthenticator.AuthenticationState,
        });

        var parameters = ParameterView.FromDictionary(
            new Dictionary<string, object?>
            {
                [Action] = RemoteAuthenticationActions.LogIn,
            });

        // Act
        await renderer.Dispatcher.InvokeAsync<object>(() => remoteAuthenticator.SetParametersAsync(parameters));

        // Assert
        Assert.Equal(originalUrl, remoteAuthenticator.Navigation.Uri);
    }

    [Fact]
    public async Task AuthenticationManager_Login_NavigatesToLoginFailureOnError()
    {
        // Arrange
        var (remoteAuthenticator, renderer, authServiceMock) = CreateAuthenticationManager(
            "https://www.example.com/base/authentication/login",
            new InteractiveRequestOptions { Interaction = InteractionType.SignIn, ReturnUrl = "https://www.example.com/base/fetchData" }.ToState());

        authServiceMock.SignInCallback = s => Task.FromResult(new RemoteAuthenticationResult<RemoteAuthenticationState>()
        {
            Status = RemoteAuthenticationStatus.Failure,
            ErrorMessage = "There was an error trying to log in.",
        });

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [Action] = RemoteAuthenticationActions.LogIn,
        });

        // Act
        await renderer.Dispatcher.InvokeAsync<object>(() => remoteAuthenticator.SetParametersAsync(parameters));

        // Assert
        Assert.Equal("https://www.example.com/base/authentication/login-failed", remoteAuthenticator.Navigation.Uri.ToString());
        Assert.Equal("There was an error trying to log in.", remoteAuthenticator.Navigation.HistoryEntryState);
    }

    [Fact]
    public async Task AuthenticationManager_LoginCallback_ThrowsOnRedirectResult()
    {
        // Arrange
        var (remoteAuthenticator, renderer, authServiceMock) = CreateAuthenticationManager(
            "https://www.example.com/base/authentication/");

        authServiceMock.CompleteSignInCallback = s => Task.FromResult(new RemoteAuthenticationResult<RemoteAuthenticationState>()
        {
            Status = RemoteAuthenticationStatus.Redirect,
        });

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [Action] = RemoteAuthenticationActions.LogInCallback,
        });

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await renderer.Dispatcher.InvokeAsync<object?>(async () =>
            {
                await remoteAuthenticator.SetParametersAsync(parameters);
                return null;
            }));
    }

    [Fact]
    public async Task AuthenticationManager_LoginCallback_DoesNothingOnOperationCompleted()
    {
        // Arrange
        var originalUrl = "https://www.example.com/base/authentication/login-callback?code=1234";
        var (remoteAuthenticator, renderer, authServiceMock) = CreateAuthenticationManager(
            originalUrl);

        authServiceMock.CompleteSignInCallback = s => Task.FromResult(new RemoteAuthenticationResult<RemoteAuthenticationState>()
        {
            Status = RemoteAuthenticationStatus.OperationCompleted,
        });

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [Action] = RemoteAuthenticationActions.LogInCallback,
        });

        // Act
        await renderer.Dispatcher.InvokeAsync<object>(() => remoteAuthenticator.SetParametersAsync(parameters));

        // Assert
        Assert.Equal(originalUrl, remoteAuthenticator.Navigation.Uri);
    }

    [Fact]
    public async Task AuthenticationManager_LoginCallback_NavigatesToReturnUrlFromStateOnSuccess()
    {
        // Arrange
        var (remoteAuthenticator, renderer, authServiceMock) = CreateAuthenticationManager(
            "https://www.example.com/base/authentication/login-callback?code=1234");

        var fetchDataUrl = "https://www.example.com/base/fetchData";
        remoteAuthenticator.AuthenticationState.ReturnUrl = fetchDataUrl;

        authServiceMock.CompleteSignInCallback = s => Task.FromResult(new RemoteAuthenticationResult<RemoteAuthenticationState>()
        {
            Status = RemoteAuthenticationStatus.Success,
            State = remoteAuthenticator.AuthenticationState,
        });

        var loggingSucceededCalled = false;

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [Action] = RemoteAuthenticationActions.LogInCallback,
            [OnLogInSucceded] = new EventCallbackFactory().Create<RemoteAuthenticationState>(
                remoteAuthenticator,
                (state) => loggingSucceededCalled = true),
        });

        // Act
        await renderer.Dispatcher.InvokeAsync<object>(() => remoteAuthenticator.SetParametersAsync(parameters));

        // Assert
        Assert.Equal(fetchDataUrl, remoteAuthenticator.Navigation.Uri);
        Assert.True(loggingSucceededCalled);
    }

    [Fact]
    public async Task AuthenticationManager_LoginCallback_NavigatesToLoginFailureOnError()
    {
        // Arrange
        var (remoteAuthenticator, renderer, authServiceMock) = CreateAuthenticationManager(
            "https://www.example.com/base/authentication/login-callback?code=1234");

        var fetchDataUrl = "https://www.example.com/base/fetchData";
        remoteAuthenticator.AuthenticationState.ReturnUrl = fetchDataUrl;

        authServiceMock.CompleteSignInCallback = s => Task.FromResult(new RemoteAuthenticationResult<RemoteAuthenticationState>()
        {
            Status = RemoteAuthenticationStatus.Failure,
            ErrorMessage = "There was an error trying to log in",
        });

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [Action] = RemoteAuthenticationActions.LogInCallback,
        });

        // Act
        await renderer.Dispatcher.InvokeAsync<object>(() => remoteAuthenticator.SetParametersAsync(parameters));

        // Assert
        Assert.Equal(
            "https://www.example.com/base/authentication/login-failed",
            remoteAuthenticator.Navigation.Uri);

        Assert.Equal(
            "There was an error trying to log in",
            ((TestNavigationManager)remoteAuthenticator.Navigation).HistoryEntryState);
    }

    [Fact]
    public async Task AuthenticationManager_Callbacks_OnlyExecuteOncePerAction()
    {
        // Arrange
        var (remoteAuthenticator, renderer, authServiceMock) = CreateAuthenticationManager(
            "https://www.example.com/base/authentication/login-callback?code=1234");

        authServiceMock.CompleteSignInCallback = s => Task.FromResult(new RemoteAuthenticationResult<RemoteAuthenticationState>()
        {
            Status = RemoteAuthenticationStatus.Success,
        });

        authServiceMock.CompleteSignOutCallback = s => Task.FromResult(new RemoteAuthenticationResult<RemoteAuthenticationState>()
        {
            Status = RemoteAuthenticationStatus.Success,
        });

        var logInCallbackInvocationCount = 0;
        var logOutCallbackInvocationCount = 0;

        var parameterDictionary = new Dictionary<string, object?>
        {
            [Action] = RemoteAuthenticationActions.LogInCallback,
            [OnLogInSucceded] = new EventCallbackFactory().Create<RemoteAuthenticationState>(
                remoteAuthenticator,
                (state) => logInCallbackInvocationCount++),
            [OnLogOutSucceeded] = new EventCallbackFactory().Create<RemoteAuthenticationState>(
                remoteAuthenticator,
                (state) => logOutCallbackInvocationCount++),
        };

        var initialParameters = ParameterView.FromDictionary(parameterDictionary);

        parameterDictionary[Action] = RemoteAuthenticationActions.LogOutCallback;

        var finalParameters = ParameterView.FromDictionary(parameterDictionary);

        // Act
        await renderer.Dispatcher.InvokeAsync<object>(() => remoteAuthenticator.SetParametersAsync(initialParameters));
        await renderer.Dispatcher.InvokeAsync<object>(() => remoteAuthenticator.SetParametersAsync(initialParameters));

        await renderer.Dispatcher.InvokeAsync<object>(() => remoteAuthenticator.SetParametersAsync(finalParameters));
        await renderer.Dispatcher.InvokeAsync<object>(() => remoteAuthenticator.SetParametersAsync(finalParameters));

        // Assert
        Assert.Equal(1, logInCallbackInvocationCount);
        Assert.Equal(1, logOutCallbackInvocationCount);
    }

    [Fact]
    public async Task AuthenticationManager_Logout_NavigatesToReturnUrlOnSuccess()
    {
        // Arrange
        var (remoteAuthenticator, renderer, authServiceMock) = CreateAuthenticationManager(
            "https://www.example.com/base/authentication/logout",
            new InteractiveRequestOptions { Interaction = InteractionType.SignOut, ReturnUrl = "https://www.example.com/base/" }.ToState());

        authServiceMock.GetAuthenticatedUserCallback = () => new ValueTask<ClaimsPrincipal>(new ClaimsPrincipal(new ClaimsIdentity("Test")));

        authServiceMock.SignOutCallback = s => Task.FromResult(new RemoteAuthenticationResult<RemoteAuthenticationState>()
        {
            Status = RemoteAuthenticationStatus.Success,
            State = remoteAuthenticator.AuthenticationState,
        });

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [Action] = RemoteAuthenticationActions.LogOut,
        });

        // Act
        await renderer.Dispatcher.InvokeAsync<object>(() => remoteAuthenticator.SetParametersAsync(parameters));

        // Assert
        Assert.Equal("https://www.example.com/base/", remoteAuthenticator.Navigation.Uri);
    }

    [Fact]
    public async Task AuthenticationManager_Logout_NavigatesToDefaultReturnUrlWhenNoReturnUrlIsPresent()
    {
        // Arrange
        var (remoteAuthenticator, renderer, authServiceMock) = CreateAuthenticationManager(
            "https://www.example.com/base/authentication/logout");

        authServiceMock.GetAuthenticatedUserCallback = () => new ValueTask<ClaimsPrincipal>(new ClaimsPrincipal(new ClaimsIdentity("Test")));

        authServiceMock.SignOutCallback = s => Task.FromResult(new RemoteAuthenticationResult<RemoteAuthenticationState>()
        {
            Status = RemoteAuthenticationStatus.Success,
            State = remoteAuthenticator.AuthenticationState,
        });

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [Action] = RemoteAuthenticationActions.LogOut,
        });

        // Act
        await renderer.Dispatcher.InvokeAsync<object>(() => remoteAuthenticator.SetParametersAsync(parameters));

        // Assert
        Assert.Equal("https://www.example.com/base/authentication/logged-out", remoteAuthenticator.Navigation.Uri);
    }

    [Fact]
    public async Task AuthenticationManager_Logout_DoesNothingOnRedirect()
    {
        // Arrange
        var originalUrl = "https://www.example.com/base/authentication/login";
        var (remoteAuthenticator, renderer, authServiceMock) = CreateAuthenticationManager(
            originalUrl,
            new InteractiveRequestOptions { Interaction = InteractionType.SignOut, ReturnUrl = "https://www.example.com/base/fetchData" }.ToState());

        authServiceMock.GetAuthenticatedUserCallback = () => new ValueTask<ClaimsPrincipal>(new ClaimsPrincipal(new ClaimsIdentity("Test")));

        authServiceMock.SignOutCallback = s => Task.FromResult(new RemoteAuthenticationResult<RemoteAuthenticationState>()
        {
            Status = RemoteAuthenticationStatus.Redirect,
            State = remoteAuthenticator.AuthenticationState,
        });

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [Action] = RemoteAuthenticationActions.LogOut,
        });

        // Act
        await renderer.Dispatcher.InvokeAsync<object>(() => remoteAuthenticator.SetParametersAsync(parameters));

        // Assert
        Assert.Equal(originalUrl, remoteAuthenticator.Navigation.Uri);
    }

    [Fact]
    public async Task AuthenticationManager_Logout_RedirectsToFailureOnInvalidSignOutState()
    {
        // Arrange
        var (remoteAuthenticator, renderer, _) = CreateAuthenticationManager(
            "https://www.example.com/base/authentication/logout",
            new InteractiveRequestOptions { Interaction = InteractionType.SignIn, ReturnUrl = "https://www.example.com/base/fetchData" }.ToState());

        if (remoteAuthenticator.SignOutManager is TestSignOutSessionStateManager testManager)
        {
            testManager.SignOutState = false;
        }

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [Action] = RemoteAuthenticationActions.LogOut,
        });

        // Act
        await renderer.Dispatcher.InvokeAsync<object>(() => remoteAuthenticator.SetParametersAsync(parameters));

        // Assert
        Assert.Equal(
            "https://www.example.com/base/authentication/logout-failed",
            remoteAuthenticator.Navigation.Uri);

        Assert.Equal(
            "The logout was not initiated from within the page.",
            ((TestNavigationManager)remoteAuthenticator.Navigation).HistoryEntryState);
    }

    [Fact]
    public async Task AuthenticationManager_Logout_NavigatesToLogoutFailureOnError()
    {
        // Arrange
        var (remoteAuthenticator, renderer, authServiceMock) = CreateAuthenticationManager(
            "https://www.example.com/base/authentication/logout",
            new InteractiveRequestOptions { Interaction = InteractionType.SignIn, ReturnUrl = "https://www.example.com/base/fetchData" }.ToState());

        authServiceMock.GetAuthenticatedUserCallback = () => new ValueTask<ClaimsPrincipal>(new ClaimsPrincipal(new ClaimsIdentity("Test")));

        authServiceMock.SignOutCallback = s => Task.FromResult(new RemoteAuthenticationResult<RemoteAuthenticationState>()
        {
            Status = RemoteAuthenticationStatus.Failure,
            ErrorMessage = "There was an error trying to log out",
        });

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [Action] = RemoteAuthenticationActions.LogOut,
        });

        // Act
        await renderer.Dispatcher.InvokeAsync<object>(() => remoteAuthenticator.SetParametersAsync(parameters));

        // Assert
        Assert.Equal("https://www.example.com/base/authentication/logout-failed", remoteAuthenticator.Navigation.Uri.ToString());
    }

    [Fact]
    public async Task AuthenticationManager_LogoutCallback_ThrowsOnRedirectResult()
    {
        // Arrange
        var (remoteAuthenticator, renderer, authServiceMock) = CreateAuthenticationManager(
            "https://www.example.com/base/authentication/logout-callback",
            new InteractiveRequestOptions { Interaction = InteractionType.SignIn, ReturnUrl = "https://www.example.com/base/fetchData" }.ToState());

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [Action] = RemoteAuthenticationActions.LogOutCallback,
        });

        authServiceMock.CompleteSignOutCallback = s => Task.FromResult(new RemoteAuthenticationResult<RemoteAuthenticationState>()
        {
            Status = RemoteAuthenticationStatus.Redirect,
        });

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await renderer.Dispatcher.InvokeAsync<object?>(async () =>
            {
                await remoteAuthenticator.SetParametersAsync(parameters);
                return null;
            }));
    }

    [Fact]
    public async Task AuthenticationManager_LogoutCallback_DoesNothingOnOperationCompleted()
    {
        // Arrange
        var originalUrl = "https://www.example.com/base/authentication/logout-callback?code=1234";
        var (remoteAuthenticator, renderer, authServiceMock) = CreateAuthenticationManager(
            originalUrl);

        authServiceMock.CompleteSignOutCallback = s => Task.FromResult(new RemoteAuthenticationResult<RemoteAuthenticationState>()
        {
            Status = RemoteAuthenticationStatus.OperationCompleted,
        });

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [Action] = RemoteAuthenticationActions.LogOutCallback,
        });

        // Act
        await renderer.Dispatcher.InvokeAsync<object>(() => remoteAuthenticator.SetParametersAsync(parameters));

        // Assert
        Assert.Equal(originalUrl, remoteAuthenticator.Navigation.Uri);
    }

    [Fact]
    public async Task AuthenticationManager_LogoutCallback_NavigatesToReturnUrlFromStateOnSuccess()
    {
        // Arrange
        var (remoteAuthenticator, renderer, authServiceMock) = CreateAuthenticationManager(
            "https://www.example.com/base/authentication/logout-callback-callback?code=1234");

        var fetchDataUrl = "https://www.example.com/base/fetchData";
        remoteAuthenticator.AuthenticationState.ReturnUrl = fetchDataUrl;

        authServiceMock.CompleteSignOutCallback = s => Task.FromResult(new RemoteAuthenticationResult<RemoteAuthenticationState>()
        {
            Status = RemoteAuthenticationStatus.Success,
            State = remoteAuthenticator.AuthenticationState,
        });

        var loggingOutSucceededCalled = false;
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [Action] = RemoteAuthenticationActions.LogOutCallback,
            [OnLogOutSucceeded] = new EventCallbackFactory().Create<RemoteAuthenticationState>(
                remoteAuthenticator,
                (state) => loggingOutSucceededCalled = true),
        });

        // Act
        await renderer.Dispatcher.InvokeAsync<object>(() => remoteAuthenticator.SetParametersAsync(parameters));

        // Assert
        Assert.Equal(fetchDataUrl, remoteAuthenticator.Navigation.Uri);
        Assert.True(loggingOutSucceededCalled);
    }

    [Fact]
    public async Task AuthenticationManager_LogoutCallback_NavigatesToLoginFailureOnError()
    {
        // Arrange
        var (remoteAuthenticator, renderer, authServiceMock) = CreateAuthenticationManager(
            "https://www.example.com/base/authentication/logout-callback?code=1234");

        var fetchDataUrl = "https://www.example.com/base/fetchData";
        remoteAuthenticator.AuthenticationState.ReturnUrl = fetchDataUrl;

        authServiceMock.CompleteSignOutCallback = s => Task.FromResult(new RemoteAuthenticationResult<RemoteAuthenticationState>()
        {
            Status = RemoteAuthenticationStatus.Failure,
            ErrorMessage = "There was an error trying to log out",
        });

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [Action] = RemoteAuthenticationActions.LogOutCallback,
        });

        // Act
        await renderer.Dispatcher.InvokeAsync<object>(() => remoteAuthenticator.SetParametersAsync(parameters));

        // Assert
        Assert.Equal(
            "https://www.example.com/base/authentication/logout-failed",
            remoteAuthenticator.Navigation.Uri);

        Assert.Equal(
            "There was an error trying to log out",
            ((TestNavigationManager)remoteAuthenticator.Navigation).HistoryEntryState);
    }

    public static TheoryData<UIValidator> DisplaysRightUIData { get; } = new TheoryData<UIValidator>
        {
            {
                new UIValidator
                {
                    Action = "login", SetupFakeRenderAction = (validator, remoteAuthenticator) => { remoteAuthenticator.LoggingIn = validator.FakeRender; },
                }
            },
            {
                new UIValidator
                {
                    Action = "login-callback", SetupFakeRenderAction = (validator, remoteAuthenticator) => { remoteAuthenticator.CompletingLoggingIn = validator.FakeRender; },
                }
            },
            {
                new UIValidator
                {
                    Action = "login-failed", SetupFakeRenderAction = (validator, remoteAuthenticator) => { remoteAuthenticator.LogInFailed = m => builder => validator.FakeRender(builder); },
                }
            },
            {
                new UIValidator
                {
                    Action = "profile", SetupFakeRenderAction = (validator, remoteAuthenticator) => { remoteAuthenticator.LoggingIn = validator.FakeRender; },
                }
            },

            // Profile fragment overrides
            {
                new UIValidator
                {
                    Action = "profile", SetupFakeRenderAction = (validator, remoteAuthenticator) => { remoteAuthenticator.UserProfile = validator.FakeRender; },
                }
            },
            {
                new UIValidator
                {
                    Action = "register", SetupFakeRenderAction = (validator, remoteAuthenticator) => { remoteAuthenticator.LoggingIn = validator.FakeRender; },
                }
            },

            // Register fragment overrides
            {
                new UIValidator
                {
                    Action = "register", SetupFakeRenderAction = (validator, remoteAuthenticator) => { remoteAuthenticator.Registering = validator.FakeRender; },
                }
            },
            {
                new UIValidator
                {
                    Action = "logout", SetupFakeRenderAction = (validator, remoteAuthenticator) => { remoteAuthenticator.LogOut = validator.FakeRender; },
                }
            },
            {
                new UIValidator
                {
                    Action = "logout-callback", SetupFakeRenderAction = (validator, remoteAuthenticator) => { remoteAuthenticator.CompletingLogOut = validator.FakeRender; },
                }
            },
            {
                new UIValidator
                {
                    Action = "logout-failed", SetupFakeRenderAction = (validator, remoteAuthenticator) => { remoteAuthenticator.LogOutFailed = m => builder => validator.FakeRender(builder); },
                }
            },
            {
                new UIValidator
                {
                    Action = "logged-out", SetupFakeRenderAction = (validator, remoteAuthenticator) => { remoteAuthenticator.LogOutSucceeded = validator.FakeRender; },
                }
            },
        };

    [Theory]
    [MemberData(nameof(DisplaysRightUIData))]
    public async Task AuthenticationManager_DisplaysRightUI_ForEachStateAsync(UIValidator validator)
    {
        // Arrange
        var renderer = new TestRenderer(new ServiceCollection().BuildServiceProvider());
        var testNavigationManager = new TestNavigationManager("https://www.example.com/", "Some error message.", "https://www.example.com/");
        var logger = new TestLoggerFactory(new TestSink(), false).CreateLogger<RemoteAuthenticatorViewCore<RemoteAuthenticationState>>();
        var authenticator = new TestRemoteAuthenticatorView(testNavigationManager);
        authenticator.Logger = logger;
        renderer.Attach(authenticator);
        validator.SetupFakeRender(authenticator);

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [Action] = validator.Action,
        });

        // Act
        await renderer.Dispatcher.InvokeAsync<object>(() => authenticator.SetParametersAsync(parameters));

        // Assert
        Assert.True(validator.WasCalled);
    }

    [Theory]
    [MemberData(nameof(DisplaysRightUIData))]
    public async Task AuthenticationManager_DoesNotThrowExceptionOnDisplaysUI_WhenPathsAreMissing(UIValidator validator)
    {
        // Arrange
        var renderer = new TestRenderer(new ServiceCollection().BuildServiceProvider());
        var testNavigationManager = new TestNavigationManager("https://www.example.com/", "Some error message.", "https://www.example.com/");
        var logger = new TestLoggerFactory(new TestSink(), false).CreateLogger<RemoteAuthenticatorViewCore<RemoteAuthenticationState>>();
        var authenticator = new TestRemoteAuthenticatorView(new RemoteAuthenticationApplicationPathsOptions(), testNavigationManager);
        authenticator.Logger = logger;
        renderer.Attach(authenticator);
        validator.SetupFakeRender(authenticator);

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [Action] = validator.Action,
        });

        // Act
        Task result = await renderer.Dispatcher.InvokeAsync<Task>(() => authenticator.SetParametersAsync(parameters));

        // Assert
        Assert.Null(result.Exception);
    }

    public static TheoryData<UIValidator, string> DisplaysRightUIWhenPathsAreMissingData { get; } = new TheoryData<UIValidator, string>
        {
            // Profile fragment overrides
            {
                new UIValidator
                {
                    Action = "profile",
                    SetupFakeRenderAction = (validator, remoteAuthenticator) => { remoteAuthenticator.UserProfile = validator.FakeRender; },
                    RetrieveOriginalRenderAction = (validator, remoteAuthenticator) => { validator.OriginalRender = remoteAuthenticator.UserProfile; },
                },
                "ProfileNotSupportedFragment"
            },
            {
                new UIValidator
                {
                    Action = "register",
                    SetupFakeRenderAction = (validator, remoteAuthenticator) => { remoteAuthenticator.Registering = validator.FakeRender; },
                    RetrieveOriginalRenderAction = (validator, remoteAuthenticator) => { validator.OriginalRender = remoteAuthenticator.Registering!; },
                },
                "RegisterNotSupportedFragment"
            },
        };

    [Theory]
    [MemberData(nameof(DisplaysRightUIWhenPathsAreMissingData))]
    public async Task AuthenticationManager_DisplaysRightUI_WhenPathsAreMissing(UIValidator validator, string methodName)
    {
        // Arrange
        var renderer = new TestRenderer(new ServiceCollection().BuildServiceProvider());
        var testNavigationManager = new TestNavigationManager("https://www.example.com/", "Some error message.", "https://www.example.com/");
        var logger = new TestLoggerFactory(new TestSink(), false).CreateLogger<RemoteAuthenticatorViewCore<RemoteAuthenticationState>>();
        var authenticator = new TestRemoteAuthenticatorView(new RemoteAuthenticationApplicationPathsOptions(), testNavigationManager);
        authenticator.Logger = logger;
        renderer.Attach(authenticator);

        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [Action] = validator.Action,
        });

        // Act
        await renderer.Dispatcher.InvokeAsync<object>(() => authenticator.SetParametersAsync(parameters));
        validator.RetrieveOriginalRender(authenticator);
        validator.SetupFakeRender(authenticator);
        await renderer.Dispatcher.InvokeAsync<Task>(() => authenticator.SetParametersAsync(parameters));

        // Assert
        Assert.True(validator.WasCalled);
        Assert.Equal(methodName, validator.OriginalRender!.Method.Name);
    }

    public class UIValidator
    {
#pragma warning disable S3218 // Inner class members should not shadow outer class "static" or type members
        public string? Action { get; set; }
#pragma warning restore S3218 // Inner class members should not shadow outer class "static" or type members
        public Action<UIValidator, RemoteAuthenticatorViewCore<RemoteAuthenticationState>>? SetupFakeRenderAction { get; set; }
        public Action<UIValidator, RemoteAuthenticatorViewCore<RemoteAuthenticationState>>? RetrieveOriginalRenderAction { get; set; }
        public bool WasCalled { get; set; }
        public RenderFragment? OriginalRender { get; set; }
        public RenderFragment FakeRender { get; set; }

        public UIValidator() => this.FakeRender = builder => this.WasCalled = true;

        internal void SetupFakeRender(TestRemoteAuthenticatorView manager) => this.SetupFakeRenderAction!(this, manager);
        internal void RetrieveOriginalRender(TestRemoteAuthenticatorView manager) => this.RetrieveOriginalRenderAction!(this, manager);
    }

    private static
        (RemoteAuthenticatorViewCore<RemoteAuthenticationState> manager,
        TestRenderer renderer,
        TestRemoteAuthenticationService authenticationServiceMock)

        CreateAuthenticationManager(
        string currentUri,
        string? currentState = null,
        string baseUri = "https://www.example.com/base/")
    {
        var renderer = new TestRenderer(new ServiceCollection().BuildServiceProvider());
        var logger = new TestLoggerFactory(new TestSink(), false).CreateLogger<RemoteAuthenticatorViewCore<RemoteAuthenticationState>>();
        var remoteAuthenticator = new RemoteAuthenticatorViewCore<RemoteAuthenticationState>();
        remoteAuthenticator.Logger = logger;
        renderer.Attach(remoteAuthenticator);

        var navigationManager = new TestNavigationManager(
            baseUri,
            currentState,
            currentUri);
        remoteAuthenticator.Navigation = navigationManager;

        remoteAuthenticator.AuthenticationState = new RemoteAuthenticationState();
        remoteAuthenticator.ApplicationPaths = new RemoteAuthenticationApplicationPathsOptions();

        var jsRuntime = new TestJsRuntime();
        var authenticationServiceMock = new TestRemoteAuthenticationService(
            jsRuntime,
            Mock.Of<IOptionsSnapshot<RemoteAuthenticationOptions<OidcProviderOptions>>>(),
            navigationManager);

        remoteAuthenticator.SignOutManager = new TestSignOutSessionStateManager();

        remoteAuthenticator.AuthenticationService = authenticationServiceMock;
        remoteAuthenticator.AuthenticationProvider = authenticationServiceMock;
        return (remoteAuthenticator, renderer, authenticationServiceMock);
    }

    private class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager(string baseUrl, string? currentState, string currentUrl)
        {
            this.Initialize(baseUrl, currentUrl);
            this.HistoryEntryState = currentState;
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
            => this.Uri = System.Uri.IsWellFormedUriString(uri, UriKind.Absolute) ? uri : new Uri(new Uri(this.BaseUri), uri).ToString();

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
            this.Uri = System.Uri.IsWellFormedUriString(uri, UriKind.Absolute) ? uri : new Uri(new Uri(this.BaseUri), uri).ToString();
            this.HistoryEntryState = options.HistoryEntryState;
        }
    }

#pragma warning disable CS0618 // Type or member is obsolete, we keep it for now for backwards compatibility
    private class TestSignOutSessionStateManager : SignOutSessionStateManager
#pragma warning restore CS0618 // Type or member is obsolete, we keep it for now for backwards compatibility
    {
        public TestSignOutSessionStateManager()
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            : base(null)
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        {
        }

        public bool SignOutState { get; set; } = true;

        public override ValueTask SetSignOutState()
        {
            this.SignOutState = true;
            return default;
        }

        public override Task<bool> ValidateSignOutState() => Task.FromResult(this.SignOutState);
    }

    private class TestJsRuntime : IJSRuntime
    {
        public (string identifier, object[] args) LastInvocation { get; set; }
#pragma warning disable CS8614 // Nullability of reference types in type of parameter doesn't match implicitly implemented member.
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args)
        {
            this.LastInvocation = (identifier, args);
            return default;
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args)
        {
            this.LastInvocation = (identifier, args);
            return default;
        }
#pragma warning restore CS8614 // Nullability of reference types in type of parameter doesn't match implicitly implemented member.
    }

    public class TestRemoteAuthenticatorView : RemoteAuthenticatorViewCore<RemoteAuthenticationState>
    {
        public TestRemoteAuthenticatorView()
            : this(
                new RemoteAuthenticationApplicationPathsOptions()
                {
                    RemoteProfilePath = "Identity/Account/Manage",
                    RemoteRegisterPath = "Identity/Account/Register",
                },
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                null)
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        {
        }

        public TestRemoteAuthenticatorView(NavigationManager manager)
            : this(
                  new RemoteAuthenticationApplicationPathsOptions()
                {
                    RemoteProfilePath = "Identity/Account/Manage",
                    RemoteRegisterPath = "Identity/Account/Register",
                },
                  manager)
        {
        }

        public TestRemoteAuthenticatorView(RemoteAuthenticationApplicationPathsOptions applicationPaths, NavigationManager testNavigationManager)
        {
            this.ApplicationPaths = applicationPaths;
            this.Navigation = testNavigationManager;
        }

        protected override Task OnParametersSetAsync()
        {
            if (this.Action == "register" || this.Action == "profile")
            {
                return base.OnParametersSetAsync();
            }

            return Task.CompletedTask;
        }
    }

    private class TestRemoteAuthenticationService : RemoteAuthenticationService<RemoteAuthenticationState, RemoteUserAccount, OidcProviderOptions>
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public TestRemoteAuthenticationService(
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable SA1114 // Parameter list should follow declaration
            IJSRuntime jsRuntime,
#pragma warning restore SA1114 // Parameter list should follow declaration
            IOptionsSnapshot<RemoteAuthenticationOptions<OidcProviderOptions>> options,
#pragma warning disable SA1128 // Put constructor initializers on their own line
            TestNavigationManager navigationManager)
            : base(jsRuntime, options, navigationManager, new AccountClaimsPrincipalFactory<RemoteUserAccount>(Mock.Of<IAccessTokenProviderAccessor>()), null)
#pragma warning restore SA1128 // Put constructor initializers on their own line
        {
        }

        public Func<RemoteAuthenticationContext<RemoteAuthenticationState>, Task<RemoteAuthenticationResult<RemoteAuthenticationState>>> SignInCallback { get; set; }
        public Func<RemoteAuthenticationContext<RemoteAuthenticationState>, Task<RemoteAuthenticationResult<RemoteAuthenticationState>>> CompleteSignInCallback { get; set; }
        public Func<RemoteAuthenticationContext<RemoteAuthenticationState>, Task<RemoteAuthenticationResult<RemoteAuthenticationState>>> SignOutCallback { get; set; }
        public Func<RemoteAuthenticationContext<RemoteAuthenticationState>, Task<RemoteAuthenticationResult<RemoteAuthenticationState>>> CompleteSignOutCallback { get; set; }
        public Func<ValueTask<ClaimsPrincipal>> GetAuthenticatedUserCallback { get; set; }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync() => new AuthenticationState(await this.GetAuthenticatedUserCallback());

        public override Task<RemoteAuthenticationResult<RemoteAuthenticationState>> CompleteSignInAsync(RemoteAuthenticationContext<RemoteAuthenticationState> context) => this.CompleteSignInCallback(context);

        protected internal override ValueTask<ClaimsPrincipal> GetAuthenticatedUser() => this.GetAuthenticatedUserCallback();

        public override Task<RemoteAuthenticationResult<RemoteAuthenticationState>> CompleteSignOutAsync(RemoteAuthenticationContext<RemoteAuthenticationState> context) => this.CompleteSignOutCallback(context);

        public override Task<RemoteAuthenticationResult<RemoteAuthenticationState>> SignInAsync(RemoteAuthenticationContext<RemoteAuthenticationState> context) => this.SignInCallback(context);

        public override Task<RemoteAuthenticationResult<RemoteAuthenticationState>> SignOutAsync(RemoteAuthenticationContext<RemoteAuthenticationState> context) => this.SignOutCallback(context);
    }

#pragma warning disable BL0006 // Do not use RenderTree types
    private class TestRenderer : Renderer
    {
        public TestRenderer(IServiceProvider services)
            : base(services, NullLoggerFactory.Instance)
        {
        }

#pragma warning disable S3241 // Methods should not return values that are never used
        public int Attach(IComponent component) => this.AssignRootComponentId(component);
#pragma warning restore S3241 // Methods should not return values that are never used

        private static readonly Dispatcher DispatcherValue = Dispatcher.CreateDefault();

        public override Dispatcher Dispatcher => DispatcherValue;

        protected override void HandleException(Exception exception)
            => ExceptionDispatchInfo.Capture(exception).Throw();

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) =>
            Task.CompletedTask;
    }
#pragma warning restore BL0006 // Do not use RenderTree types
}
#pragma warning restore BL0005 // Component parameter should not be set outside of its component.