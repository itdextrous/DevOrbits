import CustomerSearchDto from "Models/Customer/CustomerSearchDto";
import {createContext, ReactNode, useContext, useState} from "react";

interface RecentCustomerProvider {
  readonly recentCustomers: CustomerSearchDto[];
  addRecentCustomer: (customer: CustomerSearchDto) => void,
}

const recentCustomerContext = createContext<RecentCustomerProvider>({
  // Empty implementation of RecentCustomerProvider
  recentCustomers: [],
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  addRecentCustomer(customer: CustomerSearchDto) { },
});

const MaximumRecentCustomers = 10;

interface Props {
  children: ReactNode
}

export function ProvideRecentCustomers({ children }: Props) {
  const [recentCustomers, setRecentCustomers] = useState<CustomerSearchDto[]>([]);

  function addRecentCustomer(customer: CustomerSearchDto) {
    setRecentCustomers((currentState) => {
      // Copy so react knows the array is changing
      const customers = Array.from(currentState);
      let foundIndex = customers.findIndex((item) => item.customerId === customer.customerId);
      while (foundIndex >= 0) {
        // Remove customer if they are already in the list so they get added at the top
        customers.splice(foundIndex, 1);
        foundIndex = customers.findIndex((item) => item.customerId === customer.customerId);
      }
      const length = customers.unshift(customer);
      if (length > MaximumRecentCustomers) {
        customers.length = MaximumRecentCustomers;
      }
      return customers;
    });
  }

  return (
    <recentCustomerContext.Provider  value={{recentCustomers,  addRecentCustomer}}>
      {children}
    </recentCustomerContext.Provider>
  );
}

export function useRecentCustomers() {
  return useContext(recentCustomerContext);
}