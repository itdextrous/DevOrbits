import { useHistory } from "react-router-dom";
import Inbox from "Components/Messages/Inbox";
import { Button, Col } from "react-bootstrap";
import useSendMessageModal from "Components/Modals/SendMessageModal";
import messageApi from "Services/MessageApi";
import MessageToCustomerDto from "Models/Message/MessageToCustomerDto";
import { useState } from "react";

export default function BrokerInbox(props:any) {
  const [data, setData] = useState(false) 
  const history = useHistory();
  const { SendMessageModal, setIsMessageModalVisible, modalProps, setIsToVisible,setIsCustomerVisible } = useSendMessageModal();

  async function handleSendMessage(message: string,to: string) {
    await messageApi.toCustomer(new MessageToCustomerDto(message,to));
    setData(true)
  }
  return (
    <div>
     <Inbox onConversationClick={(conversationId) => {
        history.push(`/broker/messages/conversation/${conversationId}`);
      }}
      roleName={"Broker"}
      isRefresh={data}
      />
      <Col>
        <Button type="submit" style={{ marginTop: "6px" }} onClick={() => {
          setIsMessageModalVisible(true);
          setIsToVisible(true);
          setIsCustomerVisible(false);
        }}>Create New Conversation</Button>
      </Col>
      <SendMessageModal
        onSendClick={handleSendMessage}
        {...modalProps}
      />
    </div>
  );
}
