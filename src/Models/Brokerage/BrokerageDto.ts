import AuditableEntity from "Models/AuditableEntity";

export default class BrokerageDto extends AuditableEntity {
  brokerageId: string = "";

  name: string = "";

  phone: string | null = null;

  address: string | null = null;

  suburb: string | null = null;

  state: string | null = null;

  postcode: string | null = null;

  creditLicence: string | null = null;

  authorisedCreditRep: string | null = null;

  numberOfBrokers: string | null = null;

  aggregatorPartner: string | null = null;

  notes: string = "";
}
