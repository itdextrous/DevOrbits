import DashboardItem from "Components/Navigation/DashboardItem";
import { Col, Container, Row } from "react-bootstrap";

export default function Home() {
  return (
    <>
      <div>
        <Container>
          <h1>Dealer home</h1>
          <Row>
            <Col>
              <DashboardItem
                title="Messages"
                description="View your messages"
                linkLabel="Inbox"
                linkTo="/dealer/messages/conversations"
              />
            </Col>

            <Col>
              <DashboardItem
                title="Stock"
                description="View stock"
                linkLabel="View"
                linkTo="/dealer/stock/search"
              />
            </Col>

          </Row>
        </Container>
      </div>

    </>
  );
}
