/* eslint-disable react/jsx-props-no-spreading */
import useSelectCustomerModal from "Components/Modals/SelectCustomerModal";
import useSendMessageModal from "Components/Modals/SendMessageModal";
import StockCard from "Components/Stock/StockCard";
import notification from "Components/Utility/Notifications";
import CustomerSearchDto from "Models/Customer/CustomerSearchDto";
import MessageAboutStockDto from "Models/Message/MessageAboutStockDto";
import SavedStockDto from "Models/SavedStock/SavedStockDto";
import { IStockDto } from "Models/Stock/IStock";
import { useHistory } from "react-router-dom";
import messageApi from "Services/MessageApi";
import savedStockApi from "Services/SavedStockApi";

interface Props {
  stock: IStockDto;
  imageWidth: number;
  imageHeight: number;
  viewButtonIsVisible: boolean;
  onSaveStateChanged: (stock: IStockDto) => Promise<void>;
}

export default function BrokerStockCard({stock, imageWidth, imageHeight, viewButtonIsVisible, onSaveStateChanged}: Props) {
  const history = useHistory();
  const { SendMessageModal, setIsMessageModalVisible, modalProps ,setIsCustomerVisible} = useSendMessageModal();
  const { SelectCustomerModal, setIsSelectCustomerModalVisible, modalProps: saveModalProps } = useSelectCustomerModal();

  function handleClickViewDetails(viewStock: IStockDto) {
    history.push(`/broker/vehicle/detail/${viewStock.stockId}`);
  }

  function handleClickSave() {
    setIsSelectCustomerModalVisible(true);
  }

  async function handleCustomerSelected(customer: CustomerSearchDto) {
    const savedStock = new SavedStockDto();
    savedStock.customerId = customer.customerId;
    savedStock.stockId = stock.stockId;
    await savedStockApi.recommend(savedStock);
    notification.success("Vehicle added to customer shortlist.");
  }

  function handleClickEnquire() {
    setIsCustomerVisible(true);
    setIsMessageModalVisible(true);
  }
  async function handleSendMessage(message: string,to: string,customerId: string) {
    const dto = new MessageAboutStockDto(stock.stockId, message,customerId);
    await messageApi.aboutStock(dto);
  }
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  function handleClickSecure(secureStock: IStockDto) {
    notification.info("MOCKUP - Customer asked for Vehicle deposit payment.");
  }

  return (
    <>
      <StockCard
        stock={stock}
        width={imageWidth}
        height={imageHeight}
        onClickViewDetails={viewButtonIsVisible ? handleClickViewDetails : undefined}
        onClickEnquire={handleClickEnquire}
        onClickSave={async (saveStock) => {
          handleClickSave();
          await onSaveStateChanged(saveStock);
        }}
        onClickUnsave={async (saveStock) => {
          await onSaveStateChanged(saveStock);
        }}
        onClickSecure={handleClickSecure}
      />

      <SendMessageModal
        onSendClick={handleSendMessage}
        {...modalProps}
      />  

      <SelectCustomerModal
        {...saveModalProps}
        onCustomerSelected={handleCustomerSelected}
      />
    </>
  );
}
