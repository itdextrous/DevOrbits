import AuditableEntity from "Models/AuditableEntity";

export default class BrokerageApplicationSearchDto extends AuditableEntity {
  brokerageApplicationId: string = "";

  brokerageName: string = "";

  phone: string | null = null;

  address: string | null = null;

  suburb: string | null = null;

  state: string | null = null;

  postcode: string | null = null;

  numberOfBrokers: string | null = null;

  aggregatorPartner: string | null = null;

  firstName: string = "";

  lastName: string = "";

  adminEmail: string = "";

  adminPhone: string | null = null;

  creditLicence: string | null = null;

  authorisedCreditRep: string | null = null;
}
