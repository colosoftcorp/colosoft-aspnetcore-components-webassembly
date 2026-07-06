export interface AccessToken {
  value: string;
  expires: Date;
  grantedScopes: string[];
}
