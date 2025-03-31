import Icon from "Components/Icon";
import React from "react";
import { useHistory } from "react-router-dom";

interface Props {
  author: string;
  date: string;
  snippet: string;
  imageUrl?: string;
  onClick: (e: React.MouseEvent<HTMLDivElement, MouseEvent>) => void;
}

const defaultProps = {
  imageUrl: undefined,
};

export default function MessageListItem({
  author, date, snippet, imageUrl, onClick,
}: Props) {
  const history = useHistory();

  function handleClick() {
    history.push("/public/mockup/messages/conversation/dealer");
  }

  return (
    <li className="list-group-item">

      <div
        className="media align-items-center"
        onClick={onClick}
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
            )
              : (
                <div className="avatar avatar-lg bg-primary">
                  <Icon icon="envelope" size="2x" />
                </div>
              )}

          </li>
        </ul>
        <div className="media-body">
          <div>
            <span className="h6">{author} </span>
            <span style={{ float: "right" }}>{date}</span>
          </div>
          <span className="text-small">{snippet}...</span>
        </div>
      </div>
    </li>
  );
}
MessageListItem.defaultProps = defaultProps;
