import SearchOptions from "Common/SearchOptions";
import ConversationsForCurrentUserDto from "Models/Conversation/ConversationsForCurrentUserDto";

export default class ConversationsForCurrentUserResultsDto {
  conversationList: ConversationsForCurrentUserDto[] = [];
  options: SearchOptions = new SearchOptions();
}
