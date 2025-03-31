/* eslint-disable react/jsx-props-no-spreading */
import useSendMessageModal from "Components/Modals/SendMessageModal";
import StockCard from "Components/Stock/StockCard";
import notification from "Components/Utility/Notifications";
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

export default function CustomerStockCard({stock, imageWidth, imageHeight, viewButtonIsVisible, onSaveStateChanged}: Props) {
  const history = useHistory();
  const { SendMessageModal, setIsMessageModalVisible, modalProps,setIsCustomerVisible } = useSendMessageModal();

  function handleClickViewDetails(viewStock: IStockDto) {
    history.push(`/customer/vehicle/detail/${viewStock.stockId}`);
    console.log(setIsCustomerVisible);
  }

  async function handleClickSave(saveStock: IStockDto) {
    const savedStock = new SavedStockDto();
    savedStock.stockId = saveStock.stockId;
    await savedStockApi.add(savedStock);
    notification.success("Vehicle saved to your shortlist.");
  }

  async function handleClickUnsave(unsaveStock: IStockDto) {
    await savedStockApi.delete(unsaveStock.stockId);
    notification.info("Vehicle removed from your shortlist.");
  }

  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  function handleClickEnquire(msgStock: IStockDto) {
    setIsMessageModalVisible(true);
  }
  async function handleSendMessage(message: string,to: string,customerId: string) {
    const dto = new MessageAboutStockDto(stock.stockId, message,customerId);
    await messageApi.aboutStock(dto);
  }
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  function handleClickSecure(secureStock: IStockDto) {
    history.push(`/customer/vehicle/secure/${secureStock.stockId}`);
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
          await handleClickSave(saveStock);
          await onSaveStateChanged(saveStock);
        }}
        onClickUnsave={async (saveStock) => {
          await handleClickUnsave(saveStock);
          await onSaveStateChanged(saveStock);
        }}
        onClickSecure={handleClickSecure}
      />
      <SendMessageModal
        onSendClick={handleSendMessage}
        {...modalProps}
      />
    </>
  );
}
