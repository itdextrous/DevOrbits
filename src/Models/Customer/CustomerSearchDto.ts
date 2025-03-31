import AuditableEntity from "Models/AuditableEntity";

export default class CustomerSearchDto extends AuditableEntity {
  customerId: string = "";

  firstName: string = "";

  lastName: string = "";

  brokerId: string = "";

  email: string = "";

  phone: string | null = null;

  address: string | null = null;

  suburb: string | null = null;

  state: string | null = null;

  postcode: string | null = null;

  applicationStatus: number = 0;

  approvedDate: string | null = null;

  financier: string | null = null;

  approvalAmount: number | null = null;

  vehicleRequirements: string | null = null;

  budget: string | null = null;

  availableDeposit: number | null = null;

  shortlistSubmittedDate: string | null = null;

  status: number = 0;

  inviteCode: string | null = null;

  sortOrder: number = 0;
}
