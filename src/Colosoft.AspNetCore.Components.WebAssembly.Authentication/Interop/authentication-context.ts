import { InteractiveAuthenticationRequest } from './interactive-authentication-request';

export interface AuthenticationContext {
  state?: unknown;
  interactiveRequest: InteractiveAuthenticationRequest;
}
