import { useQuery } from "react-query";
import conversationApi from "Services/ConversationApi";
import { parseISO, formatDistanceToNow } from "date-fns";
import InboxConversation from "./InboxConversation";
import { oidcUserManager } from "Services/Security/OidcUserManager";

interface Props {
  onConversationClick: (conversationId: string) => void;
   roleName : string;
   isRefresh:boolean;
}

let userRole: string;
async function refreshUser() {
  const updatedUser = await oidcUserManager.getUser();
  userRole = updatedUser?.profile.role;
}


export default function Inbox({ onConversationClick, isRefresh }: Props) {
refreshUser();
  const query = useQuery(["ConversationsForCurrentUser"], () => conversationApi.lastConversationforCurrentUser());
  const brokerConvo = query.data?.conversationList?.filter((x: any) => x.stockId === null);
  const convWithBroker = brokerConvo?.filter((x:any) => x.subject !== "Website Feedback");

  const convWithDealer = query.data?.conversationList?.filter((x: any) => x.stockId !== null);
if(isRefresh){
  query.refetch();
}
  return (
    <>
      <ol className="list-group list-group-activity">
   
        { convWithBroker !== undefined ? convWithBroker.length !== 0 ? (userRole==="Broker" ||userRole==="Customer")? <h4>Conversation with Broker</h4> : null : null:null}
        { convWithBroker !== undefined ? convWithBroker.length !== 0 ?(userRole==="Broker"||userRole==="Customer")? convWithBroker.map((convo, index) => {
            let img = "";
            if (convo.dealerId !== "0") {
              img = `https://nanimagestorage.blob.core.windows.net/images/${convo.stockImageFileName}`;
            }
            return (
              
              <>
                <InboxConversation
                                convoType ={"Broker"}
                                conversationId={convo.conversationId}
                  // subject={convo.subject | ""}
                  date={formatDistanceToNow(parseISO(convo.lastMessage))}
                  sender={convo.fromName}
                  imageUrl={img}
                  onClick={onConversationClick}
                  key={index.toString()}
                />
              </>
            );
          })
            :null
            : null
            :null
        }
        { convWithDealer !== undefined ? convWithDealer.length !== 0 ? <h4>Conversation with {(userRole==="Dealer"  ||userRole==="Broker" ) ?"Customer":"Dealer"}</h4> : null : null}
        { convWithDealer !== undefined ? convWithDealer.length !== 0 ? convWithDealer.map((convo, index) => {
            let img = "";
         
            if (convo.dealerId !== "0") {
              img = `https://nanimagestorage.blob.core.windows.net/images/${convo.stockImageFileName}`;
            }
            return (
              <>
                <InboxConversation
                  convoType ={"Dealer"}
                  conversationId={convo.conversationId}
                  subject={convo.subject || ""}
                  date={formatDistanceToNow(parseISO(convo.lastMessage))}
                  sender={convo.fromName}
                  imageUrl={img}
                  onClick={onConversationClick}
                  key={index.toString()}
                />
              </>
            );
          })
            : !convWithBroker || !convWithDealer ? <h5>No Conversations Found</h5> : null
            : null
        }
    
      </ol>
    </>
  );
}
