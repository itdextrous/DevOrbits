import { faSmileBeam } from "@fortawesome/free-solid-svg-icons";
import { useAuthentication } from "Common/Authorization/ProvideAuthorization";
import Icon from "Components/Icon";
import notification from "Components/Utility/Notifications";
import { useEffect, useState } from "react";
import { Button } from "react-bootstrap";

const Todo = () => {
  const [secondsTillExpiry, setSecondsTillExpiry] = useState(0);

  const auth = useAuthentication();
  const { user } = auth;

  useEffect(() => {
    const timerId = setInterval(() => {
      if (user) {
        const dateExp = new Date(user?.expires_at * 1000);
        const dateNow = new Date();
        const diff = Math.round((dateExp.valueOf() - dateNow.valueOf()) / 1000);

        setSecondsTillExpiry(diff < 1 ? 0 : diff);
      }
    }, 1000);

    return () => { clearInterval(timerId); };
  }, [secondsTillExpiry, user]);

  return (
    <div>
      <h1>Development Page <Icon icon={faSmileBeam} /> </h1>
      <Button
        onClick={() => {
          notification.show("Test notification");
          notification.success("Success notification");
          notification.info("Info notification - this is a long notification with many words that will take a lot of space");
          notification.warning("Warning notification");
          notification.error("Error notification");
        }}
        style={{ float: "right" }}
      >Show Notifications
      </Button>
      {user?.expires_at
        && (
          <p>Token expires: {(new Date(user?.expires_at * 1000)).toString()}<br />
            In {secondsTillExpiry} seconds (renews 60 seconds before expiry)
          </p>
        )}
      <pre>
        {JSON.stringify({ auth }, null, 2)}
      </pre>
    </div>
  );
};
export default Todo;
