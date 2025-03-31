import { Button } from "react-bootstrap";
import { useAuthentication } from "./ProvideAuthorization";

export default function AccessRestricted() {
  const auth = useAuthentication();

  return (
    <>
      <h1>Access Denied</h1>
      <p>You do not have access to this resource</p>
      <Button
        type="button"
        onClick={() => { auth.signIn() }}
      >Login
      </Button>
    </>
  );
}
