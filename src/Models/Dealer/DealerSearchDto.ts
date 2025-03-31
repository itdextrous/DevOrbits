import AuditableEntity from "Models/AuditableEntity";

export default class DealerSearchDto extends AuditableEntity {
  dealerId: string = "";

  dealershipName: string = "";

  firstName: string = "";

  lastName: string = "";

  email: string = "";

  phone: string = "";

  dealerLicence: string = "";

  dealerManagementSystem: string = "";

  address: string = "";

  suburb: string = "";

  state: string = "";

  postcode: string = "";

  stockType: number = 0;

  vehicleCount: string = "";

  sites: number = 0;
}
