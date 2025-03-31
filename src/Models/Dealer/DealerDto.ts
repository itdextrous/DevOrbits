import AuditableEntity from "Models/AuditableEntity";

export default class DealerDto extends AuditableEntity {
  dealerId: string = "";

  dealershipName: string = "";

  firstName: string = "";

  lastName: string = "";

  email: string | null = null;

  phone: string | null = null;

  dealerLicence: string | null = null;

  dealerManagementSystem: string | null = null;

  address: string | null = null;

  suburb: string | null = null;

  state: string | null = null;

  postcode: string | null = null;

  stockType: number = 0;

  vehicleCount: string | null = null;

  sites: number = 0;
}
