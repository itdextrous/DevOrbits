import { oidcUserManagerSettings } from "Services/Security/OidcUserManager";
import { UserManager } from "oidc-client";
import { useEffect } from "react";
import { useHistory } from "react-router-dom";
import { useAuthentication } from "Common/Authorization/ProvideAuthorization";

export default function OidcCallbackPage() {
  const history = useHistory();
  const auth = useAuthentication();

  useEffect(() => {
    async function processCallback() {
      // about response_mode https://github.com/IdentityModel/oidc-client-js/issues/780#issuecomment-470935675
      const mgr = new UserManager({
        ...oidcUserManagerSettings,
        response_mode: "query",
      });

      try {
        await mgr.signinRedirectCallback();

        await mgr.clearStaleState();

        await auth.refreshUser();

        history.replace("/");
      } catch (error) {
        console.error("Oidc callback error: ", error);
      }
    }

    processCallback();
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <div>
      <p>Processing login...</p>
      <p><a href="/">Go to Dashboard</a></p>
    </div>
  );
}
