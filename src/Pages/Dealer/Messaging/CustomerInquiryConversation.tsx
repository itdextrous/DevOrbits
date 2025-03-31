import Conversation from "Components/Messages/Conversation";

export default function CustomerInquiryConversation() {
  return (
    <Conversation
      showVehicle
      thirdPartyName="Dealer"
      urlParam = "/Dealer/Stock/Detail"
    />
  );
}
