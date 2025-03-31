import { faHeart } from "@fortawesome/free-solid-svg-icons";
import { formatCurrencyWithoutCents, formatNumberGeneral } from "Common/Utility/Formatting";
import Icon from "Components/Icon";
import { getStockDescription, IStockDto } from "Models/Stock/IStock";
import {
  Card, Button, Row, Col,
} from "react-bootstrap";
import StockImages from "./StockImages";

interface Props {
  stock: IStockDto;
  width: number;
  height: number;
  onClickViewDetails?: (stock: IStockDto) => void;
  onClickSave: (stock: IStockDto) => void;
  onClickUnsave: (stock: IStockDto) => void;
  onClickEnquire: (stock: IStockDto) => void;
  onClickSecure: (stock: IStockDto) => void;
}

const defaultProps = {
  onClickViewDetails: undefined,
};

export default function StockCard({stock, width, height, onClickViewDetails, onClickSave, onClickUnsave, onClickEnquire, onClickSecure}: Props) {
  return (
    <div style={{ maxWidth: `${width}px` }}>
      <Card>
        <StockImages
          width={width}
          height={height}
          stock={stock}
        />
        <Card.Body>
          <Card.Text as="div">
            <Row>
              <Col>
                <h5>{getStockDescription(stock)}</h5>
              </Col>
              <Col md="auto" className="text-right">
                <h5><b>{formatCurrencyWithoutCents(stock.price)}</b></h5>
              </Col>
            </Row>
            <Row>
              <Col>
                <ul>
                  <li>{formatNumberGeneral(stock.odometer)} {stock.isMiles ? "miles" : "km"}</li>
                  <li>{stock.transmission}</li>
                  <li>Location: {stock.location}</li>
                </ul>
              </Col>
              <Col>
                <ul>
                  <li>{stock.body}</li>
                  <li>{stock.engineSize}</li>
                  <li>Dealer {stock.isUsed ? "Used" : "New"} Car</li>
                </ul>
              </Col>
            </Row>
            <Row>
              <Col>
                <div style={{ float: "right" }}>
                  {onClickViewDetails && (
                    <Button
                      variant="primary"
                      onClick={() => { onClickViewDetails(stock); }}
                    >View
                    </Button>
                  )}
                  {stock.isSaved ? <Button variant="secondary" onClick={() => onClickUnsave(stock)}><Icon icon={faHeart} /> Saved</Button>
                    : <Button variant="secondary" onClick={() => onClickSave(stock)}>Save</Button>}

                  <Button variant="secondary" onClick={() => onClickEnquire(stock)}>Enquire</Button>
                </div>
              </Col>
            </Row>
          </Card.Text>

        </Card.Body>
      </Card>
    </div>
  );
}
StockCard.defaultProps = defaultProps;
