import SearchOptions from "Common/SearchOptions";

export default class DealerSearchCriteriaDto {
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

  stockType: number | null = null;

  vehicleCount: string = "";

  sites: number | null = null;

  options: SearchOptions = new SearchOptions();
}
