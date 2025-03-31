export default class MessageToAdminDto {
  body: string = "";

  constructor(message: string = "") {
    this.body = message;
  }
}
