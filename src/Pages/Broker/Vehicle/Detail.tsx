import StockDetail from "Components/Stock/StockDetail";
import BrokerStockCard from "./BrokerStockCard";

export default function VehicleDetail() {
  return (
    <StockDetail>
      {(stock, onSaveStateChanged) => (
        <BrokerStockCard
          key={stock.stockId}
          stock={stock}
          imageWidth={600}
          imageHeight={450}
          viewButtonIsVisible={false}
          onSaveStateChanged={onSaveStateChanged}
        />
      )}
    </StockDetail>
  );
}
