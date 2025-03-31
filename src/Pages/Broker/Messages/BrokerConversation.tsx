import Conversation from "Components/Messages/Conversation";

export default function BrokerConversation() {
  return (
    <Conversation
      showVehicle
      thirdPartyName="Broker"
      urlParam = {"/broker/vehicle/detail"}
    />
  );
}
