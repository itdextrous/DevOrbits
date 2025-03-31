import Icon from "Components/Icon";
import notification from "Components/Utility/Notifications";
import {
  Badge,
  Button, Col, Container, Form, Row,
} from "react-bootstrap";
import { useHistory } from "react-router-dom";
import AppCard from "../../../Components/Controls/AppCard";
import Message from "./Message";

export default function Conversation({ showVehicle, thirdPartyName }: { showVehicle: boolean, thirdPartyName: string }) {
  const history = useHistory();

  return (
    <AppCard>
      {showVehicle && (
        <Container>
          <Row>
            <Col md="auto" sm="auto">
              <img
                alt="..."
                className="avatar avatar-lg"
                src="/stock/846266750768460030/846266750768460044-01.jpg"
              />
            </Col>
            <Col>
              <h5>
                <a
                  href="/"
                  target="_blank"
                  onClick={(e) => {
                    e.preventDefault();
                    notification.info("Will show the vehicle details");
                  }}
                >2020 Mercedes-Benz B-Class B180 Auto
                </a>
              </h5>
              $32000 - 123456 km - NSW
            </Col>
          </Row>
        </Container>
      )}
      <Message
        name={thirdPartyName}
        time="3 days ago"
        messageHtml="I can deliver the car on the 19th. Is that ok?"
      />
      <Message
        name="Sue Williams"
        time="Yesterday"
        messageHtml="The 19th is fine for delivery."
      />
      <Message
        name={thirdPartyName}
        time="Today"
        messageHtml="Here is the information"
        attachments={["Details.doc"]}
      />
      <Container style={{ paddingTop: "1rem" }}>
        <Form className="chat-form">
          <Row>
            <Col md="auto">To: <Badge variant="primary">Sue Williams</Badge>{" "}</Col>
            {(thirdPartyName === "Dealer") && (
              <Col md="auto">Cc:{" "}
                <Badge variant="primary">Dealer</Badge>
              </Col>
            )}
            <Col />
          </Row>
          <Row>
            <Col>
              <textarea className="form-control" placeholder="Your message" rows={6} />
            </Col>
          </Row>
          <Row style={{ paddingTop: "1rem" }}>
            <Col md="auto">
              <Button variant="primary">Send</Button>
              {" "}
              <div className="file btn btn-sm btn-secondary">
                <input type="file" name="file" id="upload" hidden />
                <label htmlFor="upload" style={{ cursor: "pointer", paddingTop: "0.3rem", height: "1.2rem" }}><Icon icon="paperclip" /> Attach</label>
              </div>
            </Col>
            <Col md className="text-right">
              <Button variant="outline-secondary" onClick={() => history.goBack()}>Cancel</Button>
            </Col>
          </Row>
        </Form>
      </Container>
    </AppCard>
  );
}
