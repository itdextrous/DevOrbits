import SearchOptions from "Common/SearchOptions";
import ConversationDto from "Models/Conversation/ConversationDto";

export default class ConversationByCustomerDto {
  conversationList: ConversationDto[] = [];
  options: SearchOptions = new SearchOptions();
}
