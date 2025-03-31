import { faSearch } from "@fortawesome/free-solid-svg-icons";
import Icon from "Components/Icon";
import ConfirmationModal from "Components/Modals/ConfirmationModal";
import { debounce } from "lodash";
import CustomerKanbanCriteria from "Models/Customer/CustomerKanbanCriteria";
import CustomerSearchDto from "Models/Customer/CustomerSearchDto";
import CustomerSearchResultsDto from "Models/Customer/CustomerSearchResultsDto";
import { useRecentCustomers } from "Pages/Broker/RecentCustomerContext";
import { useEffect, useRef, useState } from "react";
import { Form } from "react-bootstrap";
import { useQuery } from "react-query";
import customerApi from "Services/CustomerApi";

interface Props {
  isSelectCustomerModalVisible: boolean;
  setIsSelectCustomerModalVisible: (value: boolean) => void;
  onCustomerSelected: (customer: CustomerSearchDto) => void;
}

function SelectCustomerModal({ isSelectCustomerModalVisible, setIsSelectCustomerModalVisible, onCustomerSelected }: Props) {
  const { recentCustomers, addRecentCustomer } = useRecentCustomers();
  const searchInputRef = useRef<HTMLInputElement>(null);
  const [searchText, setSearchText] = useState("");
  const criteria = new CustomerKanbanCriteria();
  criteria.filter = searchText;
  const query = useQuery(["CustomerSaveSearch", searchText], () => {
    if (!criteria.filter) {
      // No search filter - so return the list of customers recently interacted with
      return new Promise((resolve: (results: CustomerSearchResultsDto) => void) => {
        const initData = new CustomerSearchResultsDto();
        initData.customerList = recentCustomers;
        resolve(initData);
      });
    }
    // do search based on what was entered
    return customerApi.kanbanList(criteria);
  });

  useEffect(() => {
    // Focus the search input when the modal becomes visible
    if (isSelectCustomerModalVisible && searchInputRef?.current) {
      searchInputRef.current.focus();
    }
  }, [isSelectCustomerModalVisible]);

  async function handleCustomerSelected(customer: CustomerSearchDto) {
    addRecentCustomer(customer);
    setIsSelectCustomerModalVisible(false);
    onCustomerSelected(customer);
  }

  return (
    <ConfirmationModal
      isVisible={isSelectCustomerModalVisible}
      title="Select customer"
      okButtonText="Select"
      onOkClick={() => { setIsSelectCustomerModalVisible(false); }}
      onCancelClick={() => { setIsSelectCustomerModalVisible(false); }}
    >
      <Form onSubmit={(e) => { e.preventDefault(); }}>
        <div style={{ marginBottom: ".5rem" }}>
          <div className="input-group input-group-round">
            <div className="input-group-prepend">
              <span className="input-group-text">
                <Icon icon={faSearch} />
              </span>
            </div>
            <input
              onChange={debounce((e) => { setSearchText(e.target.value); }, 500)}
              ref={searchInputRef}
              type="search"
              className="form-control"
              placeholder="Find a customer"
              aria-label="Find a customer"
            />
          </div>
        </div>
      </Form>

      <div style={{ maxHeight: "300px", overflowY: "scroll", overflowX: "visible" }}>
        <ol className="list-group list-group-activity">
          {query?.data && query.data?.customerList?.map((cust) => (
            <CustomerCard
              key={cust.customerId}
              customer={cust}
              onClick={handleCustomerSelected}
            />
          ))}
        </ol>
      </div>
    </ConfirmationModal>
  );
}

function CustomerCard({ customer, onClick }: { customer: CustomerSearchDto; onClick: (customer: CustomerSearchDto) => void }) {
  return (
    <li className="list-group-item">
      <div className="media align-items-center">
        <div className="media-body">
          <div>
            <button
              type="button"
              className="btn"
              onClick={(e) => {
                e.preventDefault();
                onClick(customer);
              }}
            >{`${customer.firstName} ${customer.lastName}`}
            </button>
          </div>
        </div>
      </div>
    </li>
  );
}

export default function useSelectCustomerModal() {
  const [isSelectCustomerModalVisible, setIsSelectCustomerModalVisible] = useState(false);

  return {
    SelectCustomerModal, isSelectCustomerModalVisible, setIsSelectCustomerModalVisible,
    modalProps: { isSelectCustomerModalVisible, setIsSelectCustomerModalVisible },
  };
}
