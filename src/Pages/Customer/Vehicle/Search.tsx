import SaveSearchModal from "Components/Modals/SaveSearchModal";
import StockSearch from "Components/Stock/StockSearch";
import SavedSearchDto from "Models/SavedSearch/SavedSearchDto";
import notification from "Components/Utility/Notifications";
import { useState } from "react";
import savedSearchApi from "Services/SavedSearchApi";
import CustomerStockCard from "./CustomerStockCard";

export default function VehicleSearch() {
  const [isModalVisible, setIsModalVisible] = useState(false);
  const [savedSearch, setSavedSearch] = useState<SavedSearchDto | null>(null);

  function handleShowSaveSearchModal(search: SavedSearchDto) {
    setSavedSearch(search);
    setIsModalVisible(true);
  }

  function handleModalCancel() {
    setIsModalVisible(false);
    setSavedSearch(null);
  }

  async function handleSaveSearchConfirmed(search: SavedSearchDto) {
    await savedSearchApi.add(search);
    notification.success("Vehicle saved to your shortlist.");
    handleModalCancel();
  }

  return (
    <>
      <StockSearch onSaveSearch={handleShowSaveSearchModal}>
        {(stock, onSaveStateChanged) => (
          <CustomerStockCard
            stock={stock}
            imageWidth={600}
            imageHeight={450}
            viewButtonIsVisible
            onSaveStateChanged={onSaveStateChanged}
          />
        )}
      </StockSearch>
      <SaveSearchModal
        savedSearch={savedSearch || new SavedSearchDto()}
        isVisible={isModalVisible}
        onOkClick={handleSaveSearchConfirmed}
        onCancelClick={handleModalCancel}
      />
    </>
  );
}
