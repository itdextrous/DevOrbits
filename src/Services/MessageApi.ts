import MessageAboutStockDto from "Models/Message/MessageAboutStockDto";
import MessageReplyDto from "Models/Message/MessageReplyDto";
import MessageToAdminDto from "Models/Message/MessageToAdminDto";
import MessageToCustomerDto from "Models/Message/MessageToCustomerDto";
import { apiPost } from "./ApiService";

class MessageApi {
  protected resourceName = `${process.env.REACT_APP_DOMAIN}/api/Message`;

  aboutStock(entity: MessageAboutStockDto) {
    return apiPost<MessageAboutStockDto, null>(`${this.resourceName}/AboutStock`, entity);
  }

  toAdmin(entity: MessageToAdminDto) {
    return apiPost<MessageToAdminDto, null>(`${this.resourceName}/ToAdmin`, entity);
  }

  reply(entity: MessageReplyDto) {
    return apiPost<MessageReplyDto, null>(`${this.resourceName}/Reply`, entity);
  }
  toCustomer(entity: MessageToCustomerDto) {
    return apiPost<MessageToCustomerDto, null>(`${this.resourceName}/ToCustomer`, entity);
  }
  toBroker(entity: MessageToAdminDto) {
    return apiPost<MessageToAdminDto, null>(`${this.resourceName}/ToBroker`, entity);
  }
}

const messageApi = new MessageApi();
export default messageApi;
