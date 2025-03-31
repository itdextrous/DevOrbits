import { Col, Container, Row } from "react-bootstrap";
import { useHistory, useParams,Link } from "react-router-dom";
import AppCard from "Components/Controls/AppCard";
import { useQuery } from "react-query";
import conversationApi from "Services/ConversationApi";
import { formatDistanceToNow, parseISO } from "date-fns";
import { MessageRecipientType } from "Models/Message/MessageRecipientType";
import messageApi from "Services/MessageApi";
import MessageReplyDto from "Models/Message/MessageReplyDto";
import Message from "./Message";
import ReplyForm, { MessageDetail } from "./ReplyForm";
import  vehicle  from "../../Images/defaultCars.png";

export default function Conversation({ showVehicle, thirdPartyName,urlParam }: { showVehicle: boolean, thirdPartyName: string,urlParam : string}) {
  const { conversationId } = useParams<{ conversationId: string }>();
  const history = useHistory();
  const query = useQuery(["Conversation", conversationId], () => conversationApi.find(conversationId));

  async function handleSendMessage(message: MessageDetail) {
    debugger
    const reply = new MessageReplyDto();
    reply.conversationId = conversationId;
    reply.toRecipientId = message.toId;
    reply.body = message.body;
    await messageApi.reply(reply);
    await query.refetch();
  }
  if (!query?.data) {
    return (<div>Loading...</div>);
  }
  const conversation = query.data;
  const { stock, messages } = query.data;
  debugger
  const toCcRecipients = conversation.messages.length === 1 ? conversation.messages[0].recipients:
  conversation.messages[conversation.messages.length -1].recipients;

  return (
    <AppCard>
      {showVehicle && (
        <Container>
          {
          stock && (
            <Row>
              <Col md="auto" sm="auto">
                <img
                  alt="..."
                  className="avatar avatar-lg"
                  src={`${stock.imageFilename!==null?"https://nanimagestorage.blob.core.windows.net/images/"+stock.imageFilename+"":vehicle}`}
                  //{`https://nanimagestorage.blob.core.windows.net/images/${stock.imageFilename}`}
                />
              </Col>
              <Col>
                <h5>
                <Link to={`${urlParam}/${stock.stockId}`} >
                {conversation.subject  }
                 </Link>
                </h5>
                ${stock?.price} - {stock.odometer} {stock.isMiles ? "miles" : "km"} - {stock?.location}
              </Col>
            </Row>
          )
          }
        </Container>
      )}
      {
        messages.map((msg,index) => {
          const fromName = msg.recipients.find((recipient) => recipient.type === MessageRecipientType.From);
          return (
            <Message
              name={`From: ${fromName?.name || "Unknown"}`}
              time={`${formatDistanceToNow(parseISO(msg.createDate))} ago` || ""}
              messageBody={msg.body}
              key={index.toString() || ''}
            />
          );
        })
      }
      <ReplyForm
        recipients={toCcRecipients}
        onSendClick={handleSendMessage}
        onCancelClick={() => { history.goBack(); }}
        convoType={conversation.type}
      />
    </AppCard>
  );
}
