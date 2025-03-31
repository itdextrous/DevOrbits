import { ReactNode } from "react";
import { useParams } from "react-router-dom";
import {
  Row, Col, Accordion, Card, Button,
} from "react-bootstrap";
import stockApi from "Services/StockApi";
import { useQuery } from "react-query";
import AppCard from "Components/Controls/AppCard";
import { getStockDescription } from "Models/Stock/IStock";
import { formatCurrencyWithoutCents, formatNumberGeneral } from "Common/Utility/Formatting";
import StockImages from "Components/Stock/StockImages";

export default function StockDetail() {
  const { stockId } = useParams<{ stockId?: string | undefined }>();

  const query = useQuery(["DealerStockDetail", stockId], () => stockApi.find(stockId || ""), { enabled: Boolean(stockId) });

  if (!query?.data) {
    return (<>Loading...</>);
  }

  const stock = query.data;

  return (
    <>
      <AppCard title={getStockDescription(stock)}>
        <ItemRow label="Stock No.">{stock.stockNumber}</ItemRow>
        <ItemRow label="Stock Type">{stock.stockTypeId}</ItemRow>
        <ItemRow label="Yard">{stock.yardCode}</ItemRow>
        <ItemRow label="Location">{stock.location}</ItemRow>
        <ItemRow label="Make">{stock.make}</ItemRow>
        <ItemRow label="Model">{stock.model}</ItemRow>
        <ItemRow label="Body">{stock.body}</ItemRow>
        <ItemRow label="Series">{stock.series}</ItemRow>
        <ItemRow label="Badge">{stock.badge}</ItemRow>
        <ItemRow label="Year Group">{stock.yearGroup}</ItemRow>
        <ItemRow label="Colour">{stock.colour}</ItemRow>
        <ItemRow label="New/Used">{stock.isUsed ? "Used" : "New"}</ItemRow>
        <ItemRow label="Demo">{stock.isDemo ? "DEMO" : "-"}</ItemRow>
        <ItemRow label="Price">{formatCurrencyWithoutCents(stock.price)}</ItemRow>
        <ItemRow label="Price Type">{stock.priceType}</ItemRow>
        <ItemRow label="Drive Away">{formatCurrencyWithoutCents(stock.priceDriveAway)}</ItemRow>
        <ItemRow label="Ex. Govt Charges">{formatCurrencyWithoutCents(stock.priceExGovtCharges)}</ItemRow>
        <ItemRow label="Special Price">{ formatCurrencyWithoutCents(stock.specialPrice) }</ItemRow>
        <ItemRow label="Odometer">{`${formatNumberGeneral(stock.odometer)} ${stock.isMiles ? "miles" : "km"}`}</ItemRow>
        <ItemRow label="Rego">
          {stock.regoNum && (<>{stock.regoNum} </>)}
          {stock.regoState && (<>registered in {stock.regoState} </>)}
          {stock.regoExpiry && (<>expires {stock.regoExpiry}</>)}
        </ItemRow>

        <ItemRow label="Interior Colour">{stock.interiorColour}</ItemRow>
        <ItemRow label="VIN">{stock.vin}</ItemRow>
        <ItemRow label="Build Date">{stock.buildDate}</ItemRow>
        <ItemRow label="Compliance Date">{stock.complianceDate}</ItemRow>
        <ItemRow label="Engine Number">{stock.engineNumber}</ItemRow>
        <ItemRow label="NVIC">{stock.nvic}</ItemRow>
        <ItemRow label="GCM">{stock.grossCombinationMass}</ItemRow>
        <ItemRow label="GVM">{stock.grossVehicleMass}</ItemRow>
        <ItemRow label="Tare">{stock.tare}</ItemRow>

      </AppCard>
      <AppCard>
        <ItemRow label="Redbook Code">{stock.redbookCode}</ItemRow>
        <ItemRow label="Short Description">
          <Expander value={stock.shortDescription} />
        </ItemRow>
        <ItemRow label="Standard Features">
          <Expander value={stock.standardFeature} />
        </ItemRow>
        <ItemRow label="Optional Features">
          <Expander value={stock.optionFeature} />
        </ItemRow>
        <ItemRow label="Adv Description">
          <Expander value={stock.advDescription} />
        </ItemRow>
      </AppCard>

      <AppCard>
        <ItemRow label="Sleeping Capacity">{stock.sleepingCapacity}</ItemRow>
        <ItemRow label="Toilet">{stock.toilet}</ItemRow>
        <ItemRow label="Shower">{stock.shower}</ItemRow>
        <ItemRow label="Air Conditioning">{stock.airConditioning}</ItemRow>
        <ItemRow label="Fridge">{stock.fridge}</ItemRow>
        <ItemRow label="Stereo">{stock.stereo}</ItemRow>
        <ItemRow label="Gear Count">{stock.gearCount}</ItemRow>
        <ItemRow label="Engine Power">{stock.enginePower}</ItemRow>
        <ItemRow label="Power kW">{stock.powerkW}</ItemRow>
        <ItemRow label="Power Hp">{stock.powerHp}</ItemRow>
        <ItemRow label="Engine Make">{stock.engineMake}</ItemRow>
        <ItemRow label="Gps">{stock.gps}</ItemRow>
        <ItemRow label="Serial Number">{stock.serialNumber}</ItemRow>
        <ItemRow label="Wheel Size">{stock.wheelSize}</ItemRow>
        <ItemRow label="Towball Weight">{stock.towballWeight}</ItemRow>
        <ItemRow label="Warranty">{stock.warranty}</ItemRow>
        <ItemRow label="Wheels">{stock.wheels}</ItemRow>
        <ItemRow label="Axle Configuration">{stock.axleConfiguration}</ItemRow>
        <ItemRow label="Cylinders">{stock.cylinders}</ItemRow>
        <ItemRow label="Engine Size">{stock.engineSize}</ItemRow>
        <ItemRow label="Fuel Type">{stock.fuelType}</ItemRow>
        <ItemRow label="Transmission">{stock.transmission}</ItemRow>
        <ItemRow label="Drive">{stock.drive}</ItemRow>
        <ItemRow label="Seats">{stock.seats}</ItemRow>
        <ItemRow label="Doors">{stock.doors}</ItemRow>
      </AppCard>

      <AppCard>
        <StockImages
          width={800}
          height={600}
          stock={stock}
        />
      </AppCard>
    </>
  );
}

function Expander({ value }: { value: string | null | undefined; }) {
  if (!value) { return null; }

  return (
    <Accordion>
      <Card>
        <Accordion.Toggle as={Button} variant="link" eventKey="0">

          {/* <Icon icon={faEye} /> */}
          <span style={{ whiteSpace: "nowrap" }}>{`${value?.substring(0, 70)} ${"..."}`}</span>
        </Accordion.Toggle>

        <Accordion.Collapse eventKey="0">
          <Card.Body>
            <span style={{ whiteSpace: "pre-wrap" }}>{value}</span>
          </Card.Body>
        </Accordion.Collapse>
      </Card>
    </Accordion>
  );
}

function ItemRow({ label, children }: { label: string, children: ReactNode }) {
  return (
    <Row>
      <Col md={2} sm={4} xs={6}><b>{label}</b></Col>
      <Col>{children}</Col>
    </Row>
  );
}
