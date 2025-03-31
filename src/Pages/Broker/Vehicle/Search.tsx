/* eslint-disable react/jsx-props-no-spreading */
import SaveSearchModal from "Components/Modals/SaveSearchModal";
import useSelectCustomerModal from "Components/Modals/SelectCustomerModal";
import StockSearch from "Components/Stock/StockSearch";
import notification from "Components/Utility/Notifications";
import CustomerSearchDto from "Models/Customer/CustomerSearchDto";
import SavedSearchDto from "Models/SavedSearch/SavedSearchDto";
import { useState } from "react";
import savedSearchApi from "Services/SavedSearchApi";
import BrokerStockCard from "./BrokerStockCard";

export default function VehicleSearch() {
  const [isSaveSearchModalVisible, setIsSaveSearchModalVisible] = useState(false);
  const [savedSearch, setSavedSearch] = useState<SavedSearchDto | null>(null);
  const { SelectCustomerModal, setIsSelectCustomerModalVisible, modalProps: saveModalProps } = useSelectCustomerModal();

  function handleShowSaveSearchModal(search: SavedSearchDto) {
    setSavedSearch(search);
    setIsSaveSearchModalVisible(true);
  }

  function handleSaveSearchCancel() {
    setIsSaveSearchModalVisible(false);
    setSavedSearch(null);
  }

  async function handleSaveSearchConfirmed(search: SavedSearchDto) {
    setSavedSearch(search);
    setIsSaveSearchModalVisible(false);
    setIsSelectCustomerModalVisible(true);
  }

  async function handleCustomerSelected(customer: CustomerSearchDto) {
    if (!savedSearch) { return; }
    savedSearch.customerId = customer.customerId;
    await savedSearchApi.brokerAdd(savedSearch);
    notification.success("Vehicle added to customer shortlist.");
    handleSaveSearchCancel();
  }

  return (
    <>
  
    <StockSearch onSaveSearch={handleShowSaveSearchModal}>
        {(stock, onSaveStateChanged) => (
          <BrokerStockCard
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
        isVisible={isSaveSearchModalVisible}
        onOkClick={handleSaveSearchConfirmed}
        onCancelClick={handleSaveSearchCancel}
      />

      <SelectCustomerModal
        {...saveModalProps}
        onCustomerSelected={handleCustomerSelected}
      />
    </>
  );
}
