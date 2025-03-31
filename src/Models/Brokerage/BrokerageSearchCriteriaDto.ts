import SearchOptions from "Common/SearchOptions";

export default class BrokerageSearchCriteriaDto {
  brokerageId: string | null = null;

  name: string = "";

  phone: string = "";

  address: string = "";

  suburb: string = "";

  state: string = "";

  postcode: string = "";

  creditLicence: string = "";

  authorisedCreditRep: string = "";

  numberOfBrokers: string = "";

  aggregatorPartner: string = "";

  notes: string = "";

  options: SearchOptions = new SearchOptions();
}
