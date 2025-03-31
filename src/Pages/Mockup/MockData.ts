class BrokerDto {
  companyName: string = "";

  address1?: string = "";

  address2?: string = "";

  suburb?: string = "";

  state?: string = "";

  postcode?: string = "";

  phone?: string = "";

  firstName: string = "";

  lastName: string = "";

  email?: string = "";

  mobilePhone?: string = "";
}

const mockBrokers: BrokerDto[] = [
  { companyName: "Auto Loans", firstName: "Tom", lastName: "Wilson" },
  { companyName: "Finance on Wheels", firstName: "Alex", lastName: "Smith" },
  { companyName: "Speed Loans", firstName: "Gill", lastName: "Hacking" },
];

const mockCustomers: any[] = [];

export {
  mockBrokers,
  BrokerDto,
  mockCustomers,
};
