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
import { HasValidAccessTokenResult } from './has-valid-access-token-result';
import { HasValidAccessTokenRequestOptions } from './has-valid-access-token-request-options';
import { AuthenticationContext } from './authentication-context';
import { AuthenticationResultStatus } from './authentication-result-status';
import { LogLevel } from './log-level';
import { AuthorizeService } from './authorize-service';
import { InteractiveAuthenticationRequest } from './interactive-authentication-request';
import { ManagedLogger } from './managed-logger';

const terminalRenewErrors = new Set([
  'invalid_grant',
  'invalid_client',
  'unauthorized_client',
  'login_required',
  'interaction_required',
  'consent_required',
  'account_selection_required',
]);

const renewWhenRemainingRatio = 0.25;
const minimumRenewDelayInSeconds = 5;
const retryDelaysInSeconds = [5, 15, 30, 60];

export interface TokenMaintenanceCallbacks {
  onRenewFailed(message: string): void;
  onAccessTokenExpired(): void;
}

export class OidcAuthorizeService implements AuthorizeService {
  private _userManager: UserManager;
  private _logger: ManagedLogger | undefined;
  private _intialSilentSignIn: Promise<void> | undefined;
  private _renewing: Promise<User | null> | undefined;
  private _renewTimer: ReturnType<typeof setTimeout> | undefined;
  private _retryAttempt = 0;
  private _maintenance: TokenMaintenanceCallbacks | undefined;
  constructor(userManager: UserManager, logger?: ManagedLogger) {
    this._userManager = userManager;
    this._logger = logger;
  }

  startTokenMaintenance(callbacks: TokenMaintenanceCallbacks) {
    if (this._maintenance) {
      return;
    }

    this._maintenance = callbacks;
    const events = this._userManager.events;

    events.addUserLoaded((user) => {
      this._retryAttempt = 0;
      this.scheduleRenew(user);
    });
    events.addUserUnloaded(() => this.clearRenewTimer());
    events.addAccessTokenExpiring(() => {
      void this.renewInBackground('expiring');
    });
    events.addAccessTokenExpired(() => {
      void this.handleAccessTokenExpired();
    });

    document.addEventListener('visibilitychange', () => {
      if (document.visibilityState === 'visible') {
        void this.renewIfNeeded('visible');
      }
    });
    window.addEventListener('focus', () => void this.renewIfNeeded('focus'));
    window.addEventListener('online', () => void this.renewIfNeeded('online'));
    window.addEventListener('storage', (event) => {
      if (event.key && event.key.startsWith('oidc.user:')) {
        void this.reloadUserFromStore();
      }
    });

    if (this.isSignInCallback()) {
      return;
    }

    void this.reloadUserFromStore().then(() => this.renewIfNeeded('start'));
  }

  private isSignInCallback() {
    const redirectUri = this._userManager.settings.redirect_uri;
    return !!redirectUri && location.href.startsWith(redirectUri);
  }

  private async discardUser() {
    this.clearRenewTimer();
    try {
      await this._userManager.removeUser();
    } catch (error) {
      this.debug(`Removing the stored user failed '${this.getExceptionMessage(error)}'.`);
    }
  }

  async renewToken(parameters?: SigninSilentArgs): Promise<User | null> {
    if (!this._renewing) {
      this._renewing = this.renewTokenCore(parameters).finally(() => {
        this._renewing = undefined;
      });
    }

    return this._renewing;
  }

  private async renewTokenCore(
    parameters?: SigninSilentArgs,
  ): Promise<User | null> {
    const renew = async () => {
      const current = await this._userManager.getUser();
      if (
        current &&
        !parameters?.scope &&
        !this.needsRenew(current)
      ) {
        this.debug('Access token already renewed by another context.');
        await this._userManager.events.load(current, false);
        this.scheduleRenew(current);
        return current;
      }

      this.debug('Renewing the access token.');
      return await this._userManager.signinSilent(parameters);
    };

    const locks = (navigator as any)?.locks;
    if (locks && typeof locks.request === 'function') {
      const name = `oidc-renew:${this._userManager.settings.authority}:${this._userManager.settings.client_id}`;
      return await locks.request(name, renew);
    }

    return await renew();
  }

  private needsRenew(user: User) {
    if (!user.access_token || user.expired) {
      return true;
    }

    const remaining = user.expires_in ?? 0;
    return remaining <= this.getLifetime(user) * renewWhenRemainingRatio;
  }

  private getLifetime(user: User) {
    try {
      const payload = user.access_token.split('.')[1];
      if (payload) {
        const json = JSON.parse(
          atob(payload.replace(/-/g, '+').replace(/_/g, '/')),
        );
        if (typeof json.exp === 'number' && typeof json.iat === 'number') {
          const lifetime = json.exp - json.iat;
          if (lifetime > 0) {
            return lifetime;
          }
        }
      }
    } catch {
      this.debug('Access token lifetime could not be read from the token.');
    }

    return Math.max(user.expires_in ?? 0, 300);
  }

  private scheduleRenew(user: User | null) {
    this.clearRenewTimer();

    if (!user || !user.access_token) {
      return;
    }

    const remaining = user.expires_in ?? 0;
    const threshold = this.getLifetime(user) * renewWhenRemainingRatio;
    const delay = Math.max(remaining - threshold, minimumRenewDelayInSeconds);

    this.debug(`Access token renewal scheduled in ${Math.round(delay)}s.`);
    this._renewTimer = setTimeout(() => {
      void this.renewInBackground('scheduled');
    }, delay * 1000);
  }

  private scheduleRetry(user: User | null) {
    this.clearRenewTimer();

    const index = Math.min(this._retryAttempt, retryDelaysInSeconds.length - 1);
    let delay = retryDelaysInSeconds[index];
    this._retryAttempt++;

    if (user && !user.expired && user.expires_in) {
      delay = Math.min(delay, Math.max(user.expires_in / 2, 1));
    }

    this.debug(`Access token renewal retry in ${Math.round(delay)}s.`);
    this._renewTimer = setTimeout(() => {
      void this.renewInBackground('retry');
    }, delay * 1000);
  }

  private clearRenewTimer() {
    if (this._renewTimer !== undefined) {
      clearTimeout(this._renewTimer);
      this._renewTimer = undefined;
    }
  }

  private async reloadUserFromStore() {
    const user = await this._userManager.getUser();
    if (user) {
      await this._userManager.events.load(user, false);
      this.scheduleRenew(user);
    } else {
      this.clearRenewTimer();
    }
  }

  private async renewIfNeeded(reason: string) {
    if (this.isSignInCallback()) {
      return;
    }

    const user = await this._userManager.getUser();
    if (user && this.canRenew(user) && this.needsRenew(user)) {
      await this.renewInBackground(reason);
    }
  }

  private canRenew(user: User) {
    return !!(user.refresh_token || this._userManager.settings.silent_redirect_uri);
  }

  private async renewInBackground(reason: string) {
    const user = await this._userManager.getUser();
    if (!user || !this.canRenew(user)) {
      return false;
    }

    try {
      this.debug(`Background access token renewal (${reason}).`);
      await this.renewToken();
      this._retryAttempt = 0;
      return true;
    } catch (error) {
      const message = this.getExceptionMessage(error);
      this.debug(`Background access token renewal failed '${message}'.`);

      if (this.isTerminalRenewError(error)) {
        await this.discardUser();
        this._maintenance?.onRenewFailed(message);
      } else {
        this.scheduleRetry(await this._userManager.getUser());
      }

      return false;
    }
  }

  private async handleAccessTokenExpired() {
    const user = await this._userManager.getUser();
    if (user && this.canRenew(user)) {
      const renewed = await this.renewInBackground('expired');
      if (renewed) {
        return;
      }

      const current = await this._userManager.getUser();
      if (current) {
        return;
      }
    }

    this._maintenance?.onAccessTokenExpired();
  }

  private isTerminalRenewError(error: any) {
    const code = error && typeof error.error === 'string' ? error.error : null;
    return !!code && terminalRenewErrors.has(code);
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

  async checkHasValidAccessToken(
    request?: HasValidAccessTokenRequestOptions,
  ): Promise<HasValidAccessTokenResult> {
    let user = await this._userManager.getUser();
    if (user && user.expired && user.refresh_token) {
      try {
        this.debug('Stored access token expired, renewing with the refresh token.');
        user = await this.renewToken();
      } catch (e) {
        const message = this.getExceptionMessage(e);
        this.debug(`Renewing the expired access token failed '${message}'.`);
        if (this.isTerminalRenewError(e)) {
          await this.discardUser();
          user = null;
        } else {
          user = await this._userManager.getUser();
        }
      }
    }

    if (
      user &&
      hasValidAccessToken(user) &&
      hasAllScopes(request, user.scopes)
    ) {
      if (request?.validateAuthenticationServerConnection) {
        try {
          await this._userManager.metadataService.getAuthorizationEndpoint();
        } catch {
          return { hasValidAccessToken: false };
        }
      }
      return { hasValidAccessToken: true };
    }

    return { hasValidAccessToken: false };

    function hasValidAccessToken(user: User | null): user is User {
      return !!(user && user.access_token && !user.expired && user.scopes);
    }

    function hasAllScopes(
      request: HasValidAccessTokenRequestOptions | undefined,
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
        const newUser = (await this.renewToken(parameters))!;

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

        if (this.isTerminalRenewError(e)) {
          await this.discardUser();
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
