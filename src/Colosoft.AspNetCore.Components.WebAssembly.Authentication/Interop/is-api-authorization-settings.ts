import { ApiAuthorizationSettings } from './api-authorization-settings';
import { OidcAuthorizeServiceSettings } from './oidc-authorize-service-settings';

export function isApiAuthorizationSettings(
  settings: OidcAuthorizeServiceSettings,
): settings is ApiAuthorizationSettings {
  return settings.hasOwnProperty('configurationEndpoint');
}
