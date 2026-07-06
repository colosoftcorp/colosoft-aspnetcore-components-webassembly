import { type UserManagerSettings, UserManager } from 'oidc-client-ts';
import { AccessTokenRequestOptions } from './access-token-request-options';
import { AuthenticationResult } from './authentication-result';
import { AuthenticationContext } from './authentication-context';
import { OidcAuthorizeServiceSettings } from './oidc-authorize-service-settings';
import { AuthorizeServiceSettings } from './authorize-service-settings';
import { isApiAuthorizationSettings } from './is-api-authorization-settings';
import { JavaScriptLoggingOptions } from './java-script-logging-options';
import { ManagedLogger } from './managed-logger';
import { OidcAuthorizeService } from './oidc-authorize-service';
import { AuthenticationResultStatus } from './authentication-result-status';

export class AuthenticationService {
  static _infrastructureKey =
    'Colosoft.AspNetCore.Components.WebAssembly.Authentication';
  static _initialized: Promise<void>;
  static instance: OidcAuthorizeService;
  static _pendingOperations: {
    [key: string]: Promise<AuthenticationResult> | undefined;
  } = {};

  public static init(
    settings: UserManagerSettings & AuthorizeServiceSettings,
    logger: any,
  ) {
    if (!AuthenticationService._initialized) {
      AuthenticationService._initialized = AuthenticationService.initializeCore(
        settings,
        new ManagedLogger(logger),
      );
    }

    return AuthenticationService._initialized;
  }

  public static handleCallback() {
    return AuthenticationService.initializeCore();
  }

  private static async initializeCore(
    settings?: UserManagerSettings & AuthorizeServiceSettings,
    logger?: ManagedLogger,
  ) {
    const finalSettings =
      settings || AuthenticationService.resolveCachedSettings();
    const cachedLoggerOptions =
      AuthenticationService.resolveCachedLoggerOptions();
    const finalLogger =
      logger || (cachedLoggerOptions && new ManagedLogger(cachedLoggerOptions));
    if (!settings && finalSettings && !logger && finalLogger) {
      const userManager =
        AuthenticationService.createUserManagerCore(finalSettings);

      if (
        window.parent !== window &&
        !window.opener &&
        window.frameElement &&
        userManager.settings.redirect_uri &&
        location.href.startsWith(userManager.settings.redirect_uri)
      ) {
        AuthenticationService.instance = new OidcAuthorizeService(
          userManager,
          finalLogger,
        );

        AuthenticationService._initialized = (async (): Promise<void> => {
          await AuthenticationService.instance.completeSignIn(location.href);
          return;
        })();
      }
    } else if (settings && logger) {
      const userManager =
        await AuthenticationService.createUserManager(settings);
      AuthenticationService.instance = new OidcAuthorizeService(
        userManager,
        logger,
      );
      window.sessionStorage.setItem(
        `${AuthenticationService._infrastructureKey}.CachedJSLoggingOptions`,
        JSON.stringify({
          debugEnabled: logger.debug,
          traceEnabled: logger.trace,
        }),
      );
    } else {
      // HandleCallback gets called unconditionally, so we do nothing for normal paths.
      // Cached settings are only used on handling the redirect_uri path and if the settings are not there
      // the app will fallback to the default logic for handling the redirect.
    }
  }

  private static resolveCachedSettings(): UserManagerSettings | undefined {
    const cachedSettings = window.sessionStorage.getItem(
      `${AuthenticationService._infrastructureKey}.CachedAuthSettings`,
    );
    return cachedSettings ? JSON.parse(cachedSettings) : undefined;
  }

  private static resolveCachedLoggerOptions():
    | JavaScriptLoggingOptions
    | undefined {
    const cachedSettings = window.sessionStorage.getItem(
      `${AuthenticationService._infrastructureKey}.CachedJSLoggingOptions`,
    );
    return cachedSettings ? JSON.parse(cachedSettings) : undefined;
  }

  public static getUser() {
    return AuthenticationService.instance.getUser();
  }

  public static getAccessToken(options: AccessTokenRequestOptions) {
    return AuthenticationService.instance.getAccessToken(options);
  }

  public static signIn(context: AuthenticationContext) {
    return AuthenticationService.instance.signIn(context);
  }

  public static parentSilentRedirect(context: AuthenticationContext) {
    return Promise.resolve({
      status: AuthenticationResultStatus.Success,
      errorMessage: 'Parent silent redirect is not supported in this context.',
    });
  }

  public static async silentRedirect(context: AuthenticationContext) {
    if (window?.parent?.AuthenticationService) {
      return await window.parent.silentRedirectCallback(context);
    }

    return {
      status: AuthenticationResultStatus.Failure,
      errorMessage: 'Silent redirect is not supported in this context.',
    };
  }

  public static async completeSignIn(url: string) {
    let operation = this._pendingOperations[url];
    if (!operation) {
      operation = AuthenticationService.instance.completeSignIn(url);
      await operation;
      delete this._pendingOperations[url];
    }

    return operation;
  }

  public static signOut(context: AuthenticationContext) {
    return AuthenticationService.instance.signOut(context);
  }

  public static async completeSignOut(url: string) {
    let operation = this._pendingOperations[url];
    if (!operation) {
      operation = AuthenticationService.instance.completeSignOut(url);
      await operation;
      delete this._pendingOperations[url];
    }

    return operation;
  }

  private static async createUserManager(
    settings: OidcAuthorizeServiceSettings,
  ): Promise<UserManager> {
    let finalSettings: UserManagerSettings;
    if (isApiAuthorizationSettings(settings)) {
      const response = await fetch(settings.configurationEndpoint);
      if (!response.ok) {
        throw new Error(
          `Could not load settings from '${settings.configurationEndpoint}'`,
        );
      }

      const downloadedSettings = await response.json();

      finalSettings = downloadedSettings;
    } else {
      if (!settings.scope) {
        settings.scope = settings.defaultScopes.join(' ');
      }

      if (settings.response_type === null) {
        delete settings.response_type;
      }

      finalSettings = settings;
    }

    window.sessionStorage.setItem(
      `${AuthenticationService._infrastructureKey}.CachedAuthSettings`,
      JSON.stringify(finalSettings),
    );

    return AuthenticationService.createUserManagerCore(finalSettings);
  }

  private static createUserManagerCore(finalSettings: UserManagerSettings) {
    const userManager = new UserManager(finalSettings);
    userManager.events.addUserSignedOut(async () => {
      userManager.removeUser();
    });
    return userManager;
  }
}

declare global {
  interface Window {
    AuthenticationService: AuthenticationService;
    silentRedirectCallback: (context: AuthenticationContext) => Promise<{
      status: AuthenticationResultStatus;
      errorMessage?: string;
    }>;
  }
}

AuthenticationService.handleCallback();

window.AuthenticationService = AuthenticationService;
window.silentRedirectCallback = AuthenticationService.parentSilentRedirect;
