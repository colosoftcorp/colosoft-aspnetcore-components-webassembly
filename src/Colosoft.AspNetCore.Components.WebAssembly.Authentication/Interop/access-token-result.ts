import { AccessTokenResultStatus } from './access-token-result-status';
import { AccessToken } from './access-token';

export interface AccessTokenResult {
  status: AccessTokenResultStatus;
  token?: AccessToken;
}
