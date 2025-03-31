import AuditableEntity from "Models/AuditableEntity";

export default class ConversationsForCurrentUserDto extends AuditableEntity {
  conversationId: string = "";

  subject: string | null = null;

  dealerId: string = "";

  stockId: string | null = null;

  type: number = 0;

  status: number = 0;
  stockNumber: number = 0;
  stockImageFileName: string ="";

  fromName: string = "";

  lastMessage: string = "";
  role :string="";
}