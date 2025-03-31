/* eslint-disable react/jsx-props-no-spreading */
import { Button, Dropdown } from "react-bootstrap";
import { useAuthentication } from "Common/Authorization/ProvideAuthorization";
import { LinkContainer } from "react-router-bootstrap";
import useSendMessageModal from "Components/Modals/SendMessageModal";
import MessageToAdminDto from "Models/Message/MessageToAdminDto";
import messageApi from "Services/MessageApi";
import ProfileImage from "../../Images/profile.svg"

export default function UserMenu() {
  const manageUrl = `${process.env.REACT_APP_DOMAIN}/Identity/Account/Manage`;
  const signoutUrl = `${process.env.REACT_APP_DOMAIN}/Identity/Account/Logout`;
  const auth = useAuthentication();
  const { SendMessageModal, setIsMessageModalVisible, modalProps } = useSendMessageModal();

  function handleClickFeedback() {
    setIsMessageModalVisible(true);
  }
  async function handleSendMessage(message: string,to: string) {
    await messageApi.toAdmin(new MessageToAdminDto(message));
  }

  return (
    <>
      <ul className="navbar-nav">
        {!auth.isAuthenticated && (
          <li className="nav-item">
            <Button
              variant="primary"
              onClick={async () => { await auth.signIn(); }}
            >Login
            </Button>
          </li>
        )}

        {auth.isAuthenticated && (
          <li className="nav-item ">
            <Dropdown className="d-flex profile_link">
            <img alt="profile" src={ProfileImage} />
              <Dropdown.Toggle
                as="a"
                className="nav-link"
                style={{ cursor: "pointer" }}
              >Profile
              </Dropdown.Toggle>
             
              <Dropdown.Menu
                as="div"
                title="Profile"
              >
                <Dropdown.Item title="Feedback" onClick={handleClickFeedback}>Feedback</Dropdown.Item>
                <Dropdown.Divider />
                <Dropdown.Item title="Manage" href={manageUrl}>Manage</Dropdown.Item>
                <LinkContainer
                  to={signoutUrl}
                  onClick={async () => { await auth.signOut(); }}
                  exact
                >
                  <Dropdown.Item title="Logout">Log out</Dropdown.Item>
                </LinkContainer>
              </Dropdown.Menu>
            </Dropdown>
          </li>
        )}

      </ul>
      <SendMessageModal
        onSendClick={handleSendMessage}
        {...modalProps}
      />
    </>
  );
}
