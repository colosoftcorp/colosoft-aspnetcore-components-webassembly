import {
  type User,
  type SignoutRedirectArgs,
  type SigninSilentArgs,
  type SigninRedirectArgs,
  type SigninResponse,
  type SignoutResponse,
  UserManager,
  INavigator,
  IWindow,
  NavigateParams,
  NavigateResponse,
} from 'oidc-client-ts';
import { AccessTokenRequestOptions } from './access-token-request-options';
import { AccessTokenResult } from './access-token-result';
import { AccessTokenResultStatus } from './access-token-result-status';
import { AuthenticationContext } from './authentication-context';
import { AuthenticationResultStatus } from './authentication-result-status';
import { LogLevel } from './log-level';
import { AuthorizeService } from './authorize-service';
import { InteractiveAuthenticationRequest } from './interactive-authentication-request';
import { ManagedLogger } from './managed-logger';

export class OidcAuthorizeService implements AuthorizeService {
  private _userManager: UserManager;
  private _logger: ManagedLogger | undefined;
  private _intialSilentSignIn: Promise<void> | undefined;
  constructor(userManager: UserManager, logger?: ManagedLogger) {
    this._userManager = userManager;
    this._logger = logger;
  }

  async trySilentSignIn() {
    if (!this._intialSilentSignIn) {
      this._intialSilentSignIn = (async () => {
        try {
          this.debug('Beginning initial silent sign in.');
          // await this._userManager.signinSilent();
          this.debug('Initial silent sign in succeeded.');
        } catch (e) {
          if (e instanceof Error) {
            this.debug(`Initial silent sign in failed '${e.message}'`);
          }
        }
      })();
    }

    return this._intialSilentSignIn;
  }

  async getUser() {
    if (
      window.parent === window &&
      !window.opener &&
      !window.frameElement &&
      this._userManager.settings.redirect_uri &&
      !location.href.startsWith(this._userManager.settings.redirect_uri)
    ) {
      await this.trySilentSignIn();
    }

    const user = await this._userManager.getUser();
    return user && user.profile;
  }

  async getAccessToken(
    request?: AccessTokenRequestOptions,
  ): Promise<AccessTokenResult> {
    this.trace('getAccessToken', request);
    const user = await this._userManager.getUser();
    if (
      user &&
      hasValidAccessToken(user) &&
      hasAllScopes(request, user.scopes)
    ) {
      this.debug(
        `Valid access token present expiring at '${getExpiration(user.expires_in!).toISOString()}'`,
      );
      return {
        status: AccessTokenResultStatus.Success,
        token: {
          grantedScopes: user.scopes,
          expires: getExpiration(user.expires_in!),
          value: user.access_token,
        },
      };
    } else {
      try {
        const parameters =
          request && request.scopes
            ? { scope: request.scopes.join(' ') }
            : undefined;

        this.debug(
          `Provisioning a token silently for scopes '${parameters?.scope}'`,
        );
        this.trace('userManager.signinSilent', parameters);
        const newUser = (await this._userManager.signinSilent(parameters))!;

        this.debug(
          `Provisioned an access token expiring at '${getExpiration(newUser?.expires_in!).toISOString()}'`,
        );

        const result = {
          status: AccessTokenResultStatus.Success,
          token: {
            grantedScopes: newUser.scopes,
            expires: getExpiration(newUser.expires_in!),
            value: newUser.access_token,
          },
        };

        this.trace('getAccessToken-result', result);
        return result;
      } catch (e) {
        if (e instanceof Error) {
          this.debug(`Failed to provision a token silently '${e.message}'`);
        }

        return {
          status: AccessTokenResultStatus.RequiresRedirect,
        };
      }
    }

    function hasValidAccessToken(user: User | null): user is User {
      return !!(user && user.access_token && !user.expired && user.scopes);
    }

    function getExpiration(expiresIn: number) {
      const now = new Date();
      now.setTime(now.getTime() + expiresIn * 1000);
      return now;
    }

    function hasAllScopes(
      request: AccessTokenRequestOptions | undefined,
      currentScopes: string[],
    ) {
      const set = new Set(currentScopes);
      if (request && request.scopes) {
        for (const current of request.scopes) {
          if (!set.has(current)) {
            return false;
          }
        }
      }

      return true;
    }
  }

  async signIn(context: AuthenticationContext) {
    this.trace('signIn', context);
    if (!context.interactiveRequest) {
      try {
        this.debug('Silent sign in starting');
        await this._userManager.clearStaleState();
        await this._userManager.signinSilent(
          this.createArguments(undefined, context.interactiveRequest),
        );
        this.debug('Silent sign in succeeded');
        return this.success(context.state);
      } catch (silentError) {
        if (silentError instanceof Error) {
          this.debug(
            `Silent sign in failed, redirecting to the identity provider '${silentError.message}'.`,
          );
        }
        return await this.signInInteractive(context);
      }
    } else {
      this.debug('Interactive sign in starting.');
      return this.signInInteractive(context);
    }
  }

  async createSignInUrl(context: AuthenticationContext) {
    this.trace('createSignInUrl', context);
    const customNavigator = new CustomIFrameNavigator();

    try {
      const userManager2 = new UserManager(
        {
          ...this._userManager.settings,
        },
        customNavigator,
        undefined,
        customNavigator,
      );

      (userManager2 as any)._client = (this._userManager as any)._client;

      const signInArgs = this.createArguments(
        context.state,
        context.interactiveRequest,
      );
      (signInArgs as any).forceIframeAuth = true;
      await userManager2.signinRedirect(signInArgs);
      this.debug('Create sign in url succeeded');
      const response = this.success(context.state);
      (response as any).url = customNavigator.url;
      return response;
    } catch (error) {
      if (customNavigator.url) {
        const response = this.success(context.state);
        (response as any).url = customNavigator.url;
        return response;
      }
      const message = this.getExceptionMessage(error);
      this.debug(`Create sign in url failed '${message}'.`);
      return this.error(message);
    }
  }

  async signInInteractive(context: AuthenticationContext) {
    this.trace('signInInteractive', context);
    try {
      await this._userManager.clearStaleState();
      await this._userManager.signinRedirect(
        this.createArguments(context.state, context.interactiveRequest),
      );
      this.debug('Redirect sign in succeeded');
      return this.redirect();
    } catch (redirectError) {
      const message = this.getExceptionMessage(redirectError);
      this.debug(`Redirect sign in failed '${message}'.`);
      return this.error(message);
    }
  }

  async completeSignIn(url: string) {
    this.trace('completeSignIn', url);
    const requiresLogin = await this.loginRequired(url);
    const stateExists = await this.stateExists(url);
    try {
      const user = await this._userManager.signinCallback(url);
      if (window.self !== window.top) {
        return this.operationCompleted();
      } else {
        this.trace('completeSignIn-result', user);
        return this.success(user && user.state);
      }
    } catch (error) {
      if (requiresLogin || window.self !== window.top || !stateExists) {
        return this.operationCompleted();
      }

      return this.error('There was an error signing in.');
    }
  }

  async signOut(context: AuthenticationContext) {
    this.trace('signOut', context);
    try {
      if (!(await this._userManager.metadataService.getEndSessionEndpoint())) {
        await this._userManager.removeUser();
        return this.success(context.state);
      }
      await this._userManager.signoutRedirect(
        this.createArguments(context.state, context.interactiveRequest),
      );
      return this.redirect();
    } catch (redirectSignOutError) {
      const message = this.getExceptionMessage(redirectSignOutError);
      this.debug(`Sign out error '${message}'.`);
      return this.error(message);
    }
  }

  async completeSignOut(url: string) {
    this.trace('completeSignOut', url);
    try {
      if (await this.stateExists(url)) {
        const response = await this._userManager.signoutCallback(url);
        return this.success(response && response.state);
      } else {
        return this.operationCompleted();
      }
    } catch (error) {
      const message = this.getExceptionMessage(error);
      this.debug(`Complete sign out error '${message}'`);
      return this.error(message);
    }
  }

  private getExceptionMessage(error: any) {
    if (isOidcError(error)) {
      return error.error_description;
    } else if (isRegularError(error)) {
      return error.message;
    } else {
      return error.toString();
    }

    function isOidcError(
      error: any,
    ): error is SigninResponse & SignoutResponse {
      return error && error.error_description;
    }

    function isRegularError(error: any): error is Error {
      return error && error.message;
    }
  }

  private getSearchParameters(url: string) {
    const url2 = new URL(url);
    const paramsPart = !url2.search ? url2.hash : url2.search;
    return new URLSearchParams(paramsPart);
  }

  private async stateExists(url: string) {
    const stateParam = this.getSearchParameters(url).get('state');
    if (stateParam && this._userManager.settings.stateStore) {
      return await this._userManager.settings.stateStore.get(stateParam);
    } else {
      return undefined;
    }
  }

  private async loginRequired(url: string) {
    const errorParameter = this.getSearchParameters(url).get('error');
    if (errorParameter && this._userManager.settings.stateStore) {
      const error =
        await this._userManager.settings.stateStore.get(errorParameter);
      return error === 'login_required';
    } else {
      return false;
    }
  }

  private createArguments(
    state: unknown | undefined,
    interactiveRequest: InteractiveAuthenticationRequest | undefined,
  ): SignoutRedirectArgs | SigninSilentArgs | SigninRedirectArgs {
    let scope = interactiveRequest?.scopes
      ? interactiveRequest.scopes.join(' ')
      : undefined;

    if (!scope) {
      scope = this._userManager?.settings?.scope;
    }
    return {
      state,
      scope,
      ...interactiveRequest?.additionalRequestParameters,
    };
  }

  private error(message: string) {
    return {
      status: AuthenticationResultStatus.Failure,
      errorMessage: message,
    };
  }

  private success(state: unknown) {
    return { status: AuthenticationResultStatus.Success, state };
  }

  private redirect() {
    return { status: AuthenticationResultStatus.Redirect };
  }

  private operationCompleted() {
    return { status: AuthenticationResultStatus.OperationCompleted };
  }

  private debug(message: string) {
    this._logger?.log(LogLevel.Debug, message);
  }

  private trace(message: string, data: any) {
    this._logger?.log(LogLevel.Trace, `${message}: ${JSON.stringify(data)}`);
  }
}

class CustomIFrameNavigator implements INavigator {
  public url?: string;

  public prepare(params: unknown): Promise<IWindow> {
    return Promise.resolve(new CustomWindow(this));
  }

  public callback(url: string, params?: unknown): Promise<void> {
    this.url = url;
    return Promise.resolve();
  }
}

class CustomWindow implements IWindow {
  private navigator: CustomIFrameNavigator;
  constructor(navigator: CustomIFrameNavigator) {
    this.navigator = navigator;
  }
  public navigate(params: NavigateParams): Promise<NavigateResponse> {
    this.navigator.url = params.url;
    return Promise.resolve({
      url: '',
    });
  }

  public close(): void {}
}
