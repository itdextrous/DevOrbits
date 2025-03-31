/* eslint-disable react/jsx-props-no-spreading */
import StockDetail from "Components/Stock/StockDetail";
import CustomerStockCard from "./CustomerStockCard";

export default function VehicleDetail() {
  return (
    <StockDetail>
      {(stock, onSaveStateChanged) => (
        <CustomerStockCard
          stock={stock}
          imageWidth={800}
          imageHeight={600}
          viewButtonIsVisible={false}
          onSaveStateChanged={onSaveStateChanged}
        />
      )}
    </StockDetail>
  );
}
