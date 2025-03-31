/* eslint-disable max-classes-per-file */
import AuditableEntity from "Models/AuditableEntity";
import { MessageRecipientType } from "Models/Message/MessageRecipientType";
import { ConversationStatus } from "./ConversationStatus";
import { ConversationType } from "./ConversationType";

export default class ConversationDto extends AuditableEntity {
  conversationId: string = "";

  subject: string | null = null;

  type: ConversationType = 0;

  status: ConversationStatus = 0;

  stock: IConversationStockDto | null = null;

  messages: IConversationMessageDto[] = [];
}

export interface IConversationMessageDto {
  messageId: string;

  type: number;

  body: string;

  createDate: string;

  recipients: IConversationMessageRecipientDto[];

  // messageAttachmentList: MessageAttachmentDto[]  ;
}

export interface IConversationMessageRecipientDto {
  messageRecipientId: number;

  type: MessageRecipientType;

  recipientId: string;

  name: string;

  isRead: boolean;
  role: string;
}

export interface IConversationStockDto {
  stockId: number;
  dealerId: number;
  location: string;
  make: string;
  model: string;
  yearGroup: number | null;
  series: string;
  badge: string;
  price: number;
  priceType: number;
  odometer: number | null;
  isMiles: boolean;
  body: string;
  imageFilename: string;
}
