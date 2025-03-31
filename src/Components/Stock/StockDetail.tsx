import { formatCurrencyWithoutCents, formatNumberGeneral } from "Common/Utility/Formatting";
import AppCard from "Components/Controls/AppCard";
import { IStockDto } from "Models/Stock/IStock";
import StockDto from "Models/Stock/StockDto";
import { CSSProperties, ReactNode } from "react";
import { Col, Row } from "react-bootstrap";
import { useQuery } from "react-query";
import { useParams } from "react-router-dom";
import stockApi from "Services/StockApi";
import parse from 'html-react-parser';

interface Props {
  children: (stock: IStockDto, onSaveStateChanged: (stock: IStockDto) => Promise<void>) => ReactNode;
}

export default function StockDetail({ children }: Props) {
  const { stockId } = useParams<{ stockId: string; }>();
  const query = useQuery(["Stock", stockId], () => stockApi.find(stockId));
  const stock = query.data || new StockDto();
  
  async function handleSaveStateChanged() {
    await query.refetch();
  }

  return (
    <>
      {children(stock, handleSaveStateChanged)}

      <AppCard title="Description">
        <div style={{ whiteSpace: "pre-wrap" }}>
        {parse(stock.advDescription || "")}
        </div>
      </AppCard>

      <AppCard title="Details">
        <Row>
          <Col md={4} style={vehDetailStyle}>Price</Col>
          <Col md>
            {formatCurrencyWithoutCents(stock.price)}
            {stock.priceDriveAway ? " Drive Away" : " Excl. Govt. Charges"}
          </Col>
        </Row>
        <Row>
          <Col md={4} style={vehDetailStyle}>{stock.isMiles ? "Miles" : "Kilometers"}</Col>
          <Col md>{formatNumberGeneral(stock.odometer)} {stock.isMiles ? "mi" : "km"}</Col>
        </Row>
        <Row>
          <Col md={4} style={vehDetailStyle}>Colour</Col>
          <Col md>{stock.colour}</Col>
        </Row>
        <Row>
          <Col md={4} style={vehDetailStyle}>Transmission</Col>
          <Col md>{stock.transmission}</Col>
        </Row>
        <Row>
          <Col md={4} style={vehDetailStyle}>Body</Col>
          <Col md>{stock.body}</Col>
        </Row>
        <Row>
          <Col md={4} style={vehDetailStyle}>Engine</Col>
          <Col md>{stock.engineMake} {stock.engineSize}</Col>
        </Row>
        <Row>
          <Col md={4} style={vehDetailStyle}>Registration Plate</Col>
          <Col md>{stock.regoNum}</Col>
        </Row>
        <Row>
          <Col md={4} style={vehDetailStyle}>Registration Expiry</Col>
          <Col md>{stock.regoExpiry}</Col>
        </Row>
        <Row>
          <Col md={4} style={vehDetailStyle}>Build Date</Col>
          <Col md>{stock.buildDate}</Col>
        </Row>
        <Row>
          <Col md={4} style={vehDetailStyle}>Compliance Date</Col>
          <Col md>{stock.complianceDate}</Col>
        </Row>
        <Row>
          <Col md={4} style={vehDetailStyle}>Model Year</Col>
          <Col md>{stock.yearGroup}</Col>
        </Row>
        <Row>
          <Col md={4} style={vehDetailStyle}>Stock Code</Col>
          <Col md>{stock.stockNumber}</Col>
        </Row>
      </AppCard>
    </>
  );
}

const vehDetailStyle: CSSProperties = { fontWeight: "bold" };
