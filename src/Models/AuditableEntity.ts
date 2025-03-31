export default class AuditableEntity {
  createDate: string | null = null;

  modifyDate: string | null = null;

  createdBy: string = "";

  modifiedBy: string = "";
}
