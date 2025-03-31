import Conversation from "Components/Messages/Conversation";

export default function CustomerConversation() {
  return (
    <Conversation
      showVehicle
      thirdPartyName="Broker"
      urlParam = "/customer/vehicle/detail"
    />
  );
}