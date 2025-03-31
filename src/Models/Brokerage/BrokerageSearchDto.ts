import AuditableEntity from "Models/AuditableEntity";

export default class BrokerageSearchDto extends AuditableEntity {
  brokerageId: string = "";

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
}
