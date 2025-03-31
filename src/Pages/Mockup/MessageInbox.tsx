import { useHistory } from "react-router-dom";
import MessageListItem from "./Messaging/MessageListItem";

export default function MessageInbox() {
  const history = useHistory();
  const handleDealerClick = () => {
    history.push("/public/mockup/messages/conversation/dealer");
  };

  return (
    <div>
      <h4>Conversation with Customer</h4>
      <MessageListItem
        author="Sue Williams"
        date="Today"
        snippet="Direct message from customer"
        onClick={() => { history.push("/public/mockup/messages/conversation/broker"); }}
      />
      <h4 style={{ marginTop: "1rem" }}>Conversations with Dealers</h4>
      <ol className="list-group list-group-activity">
        <MessageListItem
          author="2020 Mercedes-Benz B-Class B180 Auto"
          date="Today"
          snippet="Settlement attached"
          imageUrl="/stock/846266750768460030/846266750768460044-01.jpg"
          onClick={handleDealerClick}
        />
        <MessageListItem
          author="2017 Volvo XC60 D5 R-Design Auto AWD MY18"
          date="2 days ago"
          snippet="Update on the pricing"
          imageUrl="/stock/846266750768460030/846266750768460057-01.jpg"
          onClick={handleDealerClick}
        />
        <MessageListItem
          author="2017 Mazda CX-5 Maxx Sport KF Series Auto i-ACTIV AWD"
          date="1 day ago"
          snippet="Found a car I like"
          imageUrl="/stock/846266750768460030/846266750768460059-03.jpg"
          onClick={handleDealerClick}
        />
      </ol>
    </div>
  );
}
