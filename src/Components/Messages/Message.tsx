import Icon from "Components/Icon";

interface Props {
  name: string,
  time: string,
  messageBody: string;
  attachments?: string[];
  key?:string;
}
const defaultProps = {
  attachments: undefined,
};

export default function Message({ name, time, messageBody, attachments ,key}: Props) {
  return (
    <div className="media chat-item">
      <div className="media-body">
        <div className="chat-item-title">
          <span className="chat-item-author" data-filter-by="text">{name}</span>
          <span data-filter-by="text">{time}</span>
        </div>
        <div className="chat-item-body" style={{ whiteSpace: "pre-wrap" }}>
          {messageBody}
        </div>
        {
          attachments && (
            <div className="media media-attachment">
              {attachments?.map((attachment) => (
                <>
                  <div className="avatar bg-primary">
                    <Icon icon="paperclip" title="Attached file" />
                  </div>
                  <div className="media-body">
                    <a href="/" data-filter-by="text">{attachment}</a>
                  </div>
                </>
              ))
              }
            </div>
          )
        }
      </div>
    </div>
  );
}

Message.defaultProps = defaultProps;
