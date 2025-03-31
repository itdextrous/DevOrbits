export default class MessageAboutStockDto {
  stockId: string = "";
  customerId: string = "";
  body: string = "";

  constructor(stockId: string = "", message: string = "", customerId: string = "") {
    this.stockId = stockId;
    this.body = message;
    this.customerId = customerId;
  }
}
