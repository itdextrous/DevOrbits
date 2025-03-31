import Inbox from "Components/Messages/Inbox";
import { useHistory } from "react-router-dom";
import { Button, Col } from "react-bootstrap";
import useSendMessageModal from "Components/Modals/SendMessageModal";
import messageApi from "Services/MessageApi";
import MessageToAdminDto from "Models/Message/MessageToAdminDto";
import { useState } from "react";
export default function InboxConvo() {
  const [data, setData]=useState(false)
  const history = useHistory();
  const { SendMessageModal, setIsMessageModalVisible, modalProps } = useSendMessageModal();

  async function handleSendMessage(message: string, to: string) {
    await messageApi.toBroker(new MessageToAdminDto(message));
    setData(true)
  }
  return (

    <>
      <Inbox onConversationClick={(conversationId) => {
        history.push(`/customer/messages/conversation/${conversationId}`);
      }}
        roleName={"Broker"}
        isRefresh={data}
      />
      <Col>
        <Button type="submit" style={{ marginTop: "6px" }}
          onClick={() => {
            setIsMessageModalVisible(true);
          }}>Send Message To Broker </Button>
      </Col>
      <SendMessageModal
        onSendClick={handleSendMessage}
        {...modalProps}
      />
    </>
  );
}