import AuditableEntity from "Models/AuditableEntity";

export default class SavedStockDto extends AuditableEntity {
  customerId: string = "";

  stockId: string = "";
}
