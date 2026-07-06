import { AuthenticationResultStatus } from './authentication-result-status';

export interface AuthenticationResult {
  status: AuthenticationResultStatus;
  state?: unknown;
  message?: string;
}
