import { IStockDto } from "Models/Stock/IStock";
import { Card, Carousel } from "react-bootstrap";
import Icon from "Components/Icon";
import { faCar } from "@fortawesome/free-solid-svg-icons";

interface Props {
  width: number;
  height: number;
  stock: IStockDto;
}

export default function StockImages({ width, height, stock }: Props) {
  const imageBase = `https://nanimagestorage.blob.core.windows.net/images/`;
  return (
    <Carousel interval={null} indicators={stock.imageFilenames.length <= 1 ? false : true} controls={stock.imageFilenames.length <= 1 ? false : true}>
      {
        stock === null ?
          <Icon icon={faCar} size="10x" />
          :
          stock.imageFilenames.map((filename, index) =>
            <Carousel.Item key={filename}>
              <Card.Img
                variant="top"
                width={width}
                height={height}
                src={imageBase + filename}
                alt={`Car ${index + 1}`}
                loading="lazy"
              />
            </Carousel.Item>
          )}
    </Carousel>
  );
}
