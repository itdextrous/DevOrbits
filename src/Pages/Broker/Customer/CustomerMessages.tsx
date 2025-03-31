import conversationApi from "Services/ConversationApi";
import { parseISO, formatDistanceToNow } from "date-fns";
import InboxConversation from "../../../Components/Messages/InboxConversation";
import { useEffect, useState } from "react";
//import { useHistory } from "react-router-dom";
import ConversationsForCurrentUserDto from "Models/Conversation/ConversationsForCurrentUserDto";
interface Props {
  onConversationClick: (conversationId: string) => void;
  customerId: string;
}

export default function CustomerMessages({ onConversationClick, customerId }: Props) {
  const [conversationList, setConversationList] = useState<ConversationsForCurrentUserDto[]>([]);
  //const history = useHistory();

  useEffect(() => {
    if (customerId) {
      conversationApi.findByCustomerId(customerId).then((results) => {
        setConversationList(results);
      });
    }
  }, [customerId]);


  return (
    <>
      <ol className="list-group list-group-activity">
        {conversationList &&
          conversationList.length === 0 ?
          <p>No Conversations found</p>
          :
          conversationList?.map((convo: any) => {
            let img = "";
            if (convo.stockImageFileName !== null && convo.stockImageFileName !== undefined ) {
              img = `https://nanimagestorage.blob.core.windows.net/images/${convo.stockImageFileName}`;
            }
            else {
              img = require('../../../Images/defaultCars.png').default;
            }
            return (
              <InboxConversation
                convoType=""
                conversationId={convo.conversationId}
                subject={convo.subject || ""}
                date={formatDistanceToNow(parseISO(convo.lastMessage))}
                sender={convo.fromName}
                imageUrl={img}
                onClick={onConversationClick}
              />
            );
          })
        }
      </ol>
    </>
  );
}
