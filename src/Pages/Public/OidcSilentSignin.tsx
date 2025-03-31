import { oidcUserManager } from "Services/Security/OidcUserManager";
import { useEffect } from "react";
import { useHistory } from "react-router-dom";
import { useAuthentication } from "Common/Authorization/ProvideAuthorization";

export default function OidcSilentSignin() {
  const history = useHistory();
  const auth = useAuthentication();

  useEffect(() => {
    async function doSilentSignin() {
      try {
        await oidcUserManager.signinSilent();

        await oidcUserManager.clearStaleState();

        await auth.refreshUser();

        history.replace("/");
      } catch (error) {
        console.error("Oidc SilentSignin error: ", error);
      }
    }

    doSilentSignin();
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <div>
      <p>Processing login...</p>
      <p><a href="/">Go to Dashboard</a></p>
    </div>
  );
}
