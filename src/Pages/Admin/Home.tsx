import DashboardItem from "Components/Navigation/DashboardItem";
import { Col, Container, Row } from "react-bootstrap";

export default function Home() {
  return (
    <div>
      <Container>
        <h1>
          Administrator Home Page
        </h1>
        <Row>
          <Col>
            <DashboardItem
              title="Brokerages"
              description="Manage brokerages"
              linkLabel="Manage"
              linkTo="/admin/brokerage/search"
            />
            <DashboardItem
              title="Brokerage Applications"
              description="Review application forms"
              linkLabel="Review"
              linkTo="/admin/brokerage/application/search"
            />
          </Col>
          <Col>
            <DashboardItem
              title="Dealers"
              description="Manage dealers"
              linkLabel="Manage"
              linkTo="/admin/dealer/search"
            />
            <DashboardItem
              title="Dealer Applications"
              description="Review dealer application forms"
              linkLabel="Review"
              linkTo="/admin/dealer/application/search"
            />
          </Col>
          <Col>
            <DashboardItem
              title="Messages"
              description="View your messages"
              linkLabel="Inbox"
              linkTo="/"
            />
          </Col>
        </Row>
      </Container>
    </div>
  );
}
