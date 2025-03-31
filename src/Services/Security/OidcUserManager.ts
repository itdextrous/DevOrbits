/* eslint-disable class-methods-use-this */
import {
  UserManager, UserManagerSettings, WebStorageStateStore, Log,
} from "oidc-client";

// configured in .env files
const authorityUrl = process.env.REACT_APP_DOMAIN ? process.env.REACT_APP_DOMAIN : "REACT_APP_DOMAIN=https://localhost:44322";

const oidcUserManagerSettings: UserManagerSettings = {
  userStore: new WebStorageStateStore({}),
  authority: authorityUrl,
  client_id: "spa",
  redirect_uri: `${window.location.origin}/public/openid/callback`,
  // Use Auth with PKCE
  response_type: "code",
  scope: "openid profile roles IdentityServerApi offline_access",
  post_logout_redirect_uri: `${window.location.origin}/`,
  silent_redirect_uri: `${window.location.origin}/silent-renew.html`,
  revokeAccessTokenOnSignout: true,
  automaticSilentRenew: true,
  filterProtocolClaims: true,
  loadUserInfo: true,
};

const oidcUserManager = new UserManager(oidcUserManagerSettings);

// eslint-disable-next-line @typescript-eslint/no-explicit-any
function log(...args: any[]) {
  if (process.env.NODE_ENV !== "production") {
    console.log("Oidc ", ...args);
  }
}

if (process.env.NODE_ENV !== "production") {
  Log.logger = console;
  Log.level = Log.INFO;

  oidcUserManager.events.addUserLoaded(async (user) => {
    console.log("Oidc User Loaded：", { user });
  });

  oidcUserManager.events.addAccessTokenExpiring((...args) => {
    console.log("Oidc AccessToken Expiring：", ...args);
  });
}

oidcUserManager.events.addSilentRenewError(async (...args) => {
  log("SilentRenewError：", ...args);
  await oidcUserManager.signinRedirect();
});

oidcUserManager.events.addAccessTokenExpired(async () => {
  log("AccessTokenExpired");
  let user;
  try {
    user = await oidcUserManager.signinSilent();
  } catch (error) {
    log("AccessTokenExpired error ", error);
    await oidcUserManager.signinRedirect();
  }

  if (!user) {
    log("AccessTokenExpired: Could not renew expired token. Logging you out.");

    await oidcUserManager.signoutRedirect();
  }
});

oidcUserManager.events.addUserSignedOut(async () => {
  log("UserSignedOut calling signoutRedirect()");
  await oidcUserManager.signoutRedirect();
});

export {
  oidcUserManager,
  oidcUserManagerSettings,
};
