import SearchOptions from "Common/SearchOptions";

export default class BrokerageApplicationSearchCriteriaDto {
  brokerageApplicationId: string | null = null;

  brokerageName: string | null = null;

  phone: string | null = null;

  address: string | null = null;

  suburb: string | null = null;

  state: string | null = null;

  postcode: string | null = null;

  numberOfBrokers: string | null = null;

  aggregatorPartner: string | null = null;

  firstName: string | null = null;

  lastName: string | null = null;

  adminEmail: string | null = null;

  adminPhone: string | null = null;

  creditLicence: string | null = null;

  authorisedCreditRep: string | null = null;

  options: SearchOptions = new SearchOptions();
}
