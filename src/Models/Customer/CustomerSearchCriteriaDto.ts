import SearchOptions from "Common/SearchOptions";
import AuditableEntity from "Models/AuditableEntity";

export default class CustomerSearchCriteriaDto extends AuditableEntity {
  customerId: string | null = null;

  firstName: string | null = null;

  lastName: string | null = null;

  brokerId: string | null = null;

  email: string | null = null;

  phone: string | null = null;

  address: string | null = null;

  suburb: string | null = null;

  state: string | null = null;

  postcode: string | null = null;

  applicationStatus: number | null = null;

  approvedDate: string | null = null;

  financier: string | null = null;

  approvalAmount: number | null = null;

  vehicleRequirements: string | null = null;

  budget: string | null = null;

  availableDeposit: number | null = null;

  shortlistSubmittedDate: string | null = null;

  status: number | null = null;

  inviteCode: string | null = null;

  options: SearchOptions = new SearchOptions();
}
