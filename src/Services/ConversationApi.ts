import ConversationDto from "Models/Conversation/ConversationDto";
import ConversationsForCurrentUserResultsDto from "Models/Conversation/ConversationsForCurrentUserResultsDto";
import { apiGet } from "./ApiService";
import ConversationsForCurrentUserDto from "Models/Conversation/ConversationsForCurrentUserDto";

class ConversationApi {
  protected resourceName = `${process.env.REACT_APP_DOMAIN}/api/Message/Conversation`;
  protected resourceNameConversation =`${process.env.REACT_APP_DOMAIN}/api/Message/Conversation/FindByCustomerId`;
  forCurrentUser() {
    return apiGet<ConversationsForCurrentUserResultsDto>(`${this.resourceName}/ForCurrentUser`, "");
  }
  lastConversationforCurrentUser() {
    return apiGet<ConversationsForCurrentUserResultsDto>(`${this.resourceName}/LastConversationForCurrentUser/`, "");
  }
  find(conversationId: string) {
    return apiGet<ConversationDto>(`${this.resourceName}/${conversationId}`);
  }
  findByCustomerId(customerId: string) {
    return apiGet<ConversationsForCurrentUserDto[]>(`${this.resourceNameConversation}/${customerId}`);
  }
}

const conversationApi = new ConversationApi();
export default conversationApi;
