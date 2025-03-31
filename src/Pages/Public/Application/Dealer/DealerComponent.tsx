import { Elements } from "@stripe/react-stripe-js";
import { loadStripe } from "@stripe/stripe-js";
import DealerApplication from "./Dealer";

export default function DealerComponent() {
  return (
    <>
      <Elements stripe={stripePromise}>
        <DealerApplication />
      </Elements>
    </>
  );
}
const stripePromise = loadStripe("pk_test_51JhU3mHuSqqcN6Xk5JGdsIr0v3tG1oDIYGR8rkm5dUrT3Rx2MOcuXG1OXhYVMoVh8r4GAcBMALdqlI63AuPdMwia00DW0Icn5K");
