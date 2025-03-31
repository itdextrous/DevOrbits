export default class DealerApplicationDto {
  dealerApplicationId: string = "";

  dealershipName: string = "";

  firstName: string = "";

  lastName: string | null = null;

  email: string | null = null;

  phone: string | null = null;

  dealerLicence: string = "";

  dealerManagementSystem: string | null = null;

  address: string | null = null;

  suburb: string | null = null;

  state: string | null = null;

  postcode: string | null = null;

  stockType: number = 0;

  vehicleCount: string | null = null;

  sites: number = 0;

  planName: string = "";
  
  stripeCustomerId: string = "";
}
