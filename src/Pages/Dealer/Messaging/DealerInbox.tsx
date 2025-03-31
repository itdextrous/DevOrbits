import { useHistory } from "react-router-dom";
import Inbox from "Components/Messages/Inbox";

export default function DealerInbox() {
  const history = useHistory();

  return (
    <div>
      <Inbox
        onConversationClick={(conversationId) => {
          history.push(`/dealer/messages/conversation/${conversationId}`);
        }
        }
        roleName="Broker"
        isRefresh={true}
      />
    </div>
  );
}
