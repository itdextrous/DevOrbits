import Roles from "Common/Authorization/Roles";
import Icon from "Components/Icon";

interface Props {
  conversationId: string;
  subject?: string;
  date: string;
  sender: string;
  imageUrl: string;
  convoType:string;
  onClick: (conversationId: string) => void;
}

const defaultProps = { imageUrl: undefined };

export default function InboxConversation({ conversationId, subject, date, sender, imageUrl, convoType,onClick }: Props) {
  function handleClick() {
    onClick(conversationId);
  }

  return (
    <li className="list-group-item">
      <div
        className="media align-items-center"
        onClick={() => handleClick()}
        onKeyPress={() => handleClick()}
        role="link"
        tabIndex={0}
        style={{ cursor: "pointer" }}
      >
        <ul className="avatars">
          <li>
            {imageUrl ? (
              <img
                className="avatar avatar-lg"
                width="40"
                height="40"
                src={imageUrl}
                alt="Vehicle"
              />
            ) : (
                <div className="avatar avatar-lg bg-primary">
                  <Icon icon="envelope" size="2x" />
                </div> )}

          </li>
        </ul>
        <div className="media-body">
          <div>
            <span className="h6">{subject} </span>
            <span style={{ float: "right" }}>{date}</span>
          </div>
          <span className={convoType === Roles.Dealer ? "text-small" : "h6"}>{sender}</span>
        </div>
      </div>
    </li>
  );
}
InboxConversation.defaultProps = defaultProps;
