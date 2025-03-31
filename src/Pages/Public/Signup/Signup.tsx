import { Elements } from "@stripe/react-stripe-js";
import { loadStripe } from "@stripe/stripe-js";
import { Card } from "react-bootstrap";
import SetupAccount from "./SetupAccount/SetupAccount";
import "./signup.css";

export default function SingupPublic() {
  return (
    <>
      <Elements stripe={stripePromise}>
        <Card className="top_card">
          <h5>Create your Personal Account</h5>
          <span>Setup Account</span>
        </Card>
        <SetupAccount />
      </Elements>
    </>
  );
}
const stripePromise = loadStripe("pk_test_51JhU3mHuSqqcN6Xk5JGdsIr0v3tG1oDIYGR8rkm5dUrT3Rx2MOcuXG1OXhYVMoVh8r4GAcBMALdqlI63AuPdMwia00DW0Icn5K");
