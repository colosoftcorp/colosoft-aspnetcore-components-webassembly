import { AccessTokenResult } from './access-token-result';
import { AccessTokenRequestOptions } from './access-token-request-options';
import { AuthenticationContext } from './authentication-context';
import { AuthenticationResult } from './authentication-result';

export interface AuthorizeService {
  getUser(): Promise<unknown>;
  getAccessToken(
    request?: AccessTokenRequestOptions,
  ): Promise<AccessTokenResult>;
  signIn(context: AuthenticationContext): Promise<AuthenticationResult>;
  completeSignIn(state: unknown): Promise<AuthenticationResult>;
  signOut(context: AuthenticationContext): Promise<AuthenticationResult>;
  completeSignOut(url: string): Promise<AuthenticationResult>;
}
