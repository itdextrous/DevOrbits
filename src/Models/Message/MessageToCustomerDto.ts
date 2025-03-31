export default class MessageToCustomerDto {
    body :string = "";
    to :string = "";
    constructor(message: string = "",to :string = "") {
      this.body = message;
      this.to = to;
    }
  }
  