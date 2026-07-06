import type { UserManagerSettings } from 'oidc-client-ts';
import { AuthorizeServiceSettings } from './authorize-service-settings';
import { ApiAuthorizationSettings } from './api-authorization-settings';

type Writeable<T> = { -readonly [P in keyof T]: T[P] };
type ExtendedUserManagerSettings = Writeable<
  UserManagerSettings & AuthorizeServiceSettings
>;
export type OidcAuthorizeServiceSettings =
  | ExtendedUserManagerSettings
  | ApiAuthorizationSettings;
