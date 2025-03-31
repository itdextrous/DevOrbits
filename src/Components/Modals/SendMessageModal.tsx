import ConfirmationModal from "Components/Modals/ConfirmationModal";
import CustomerRoleDto from "Models/Customer/CustomerRoleDto";
import { useEffect, useRef, useState } from "react";
import { Form } from "react-bootstrap";
import customerApi from "Services/CustomerApi";
import Icon from "Components/Icon";
import { faSearch } from "@fortawesome/free-solid-svg-icons";
import { debounce } from "lodash";
import CustomerKanbanCriteria from "Models/Customer/CustomerKanbanCriteria";
import { useQuery } from "react-query";
import CustomerSearchResultsDto from "Models/Customer/CustomerSearchResultsDto";
import { useRecentCustomers } from "Pages/Broker/RecentCustomerContext";
import CustomerSearchDto from "Models/Customer/CustomerSearchDto";
import notification from "Components/Utility/Notifications";
interface Props {
  isMessageModalVisible: boolean;
  isToVisible: boolean;
  isCustomerVisible:boolean;
  setIsMessageModalVisible: (value: boolean) => void;
  onSendClick: (message: string, to: string, customerId: string) => void;
  setIsToVisible: (value: boolean) => void;
  setIsCustomerVisible: (value: boolean) => void;
}

function SendMessageModal({ isMessageModalVisible, setIsMessageModalVisible, onSendClick, isToVisible, setIsToVisible,isCustomerVisible,setIsCustomerVisible }: Props) {
  const [message, setMessage] = useState("");
  const { recentCustomers, addRecentCustomer } = useRecentCustomers();
  const [to, setTo] = useState("");
  const searchInputRef = useRef<HTMLInputElement>(null);
  const textareaRef = useRef<HTMLTextAreaElement | null>(null);
  const [searchText, setSearchText] = useState("");
  const hasMessage = !message ? false : Boolean(message.trim());
  const [showCustomer, setShowCustomer] = useState(false);
  const [customerList, setCustomerList] = useState<CustomerRoleDto[] | null>(null);
  const criteria = new CustomerKanbanCriteria();
  criteria.filter = searchText;

  const query = useQuery(["CustomerSaveSearch", searchText], () => {
    if (!criteria.filter) {
      return new Promise((resolve: (results: CustomerSearchResultsDto) => void) => {
        const initData = new CustomerSearchResultsDto();
        initData.customerList = recentCustomers;
        resolve(initData);
      });
    }
    return customerApi.kanbanList(criteria);
  });

  useEffect(() => {
    if (isToVisible) {
      getCustomers();
    }
  }, [isToVisible]);

  function handleOkClick() {
    debugger
    let customerId = recentCustomers.length !== 0 ?recentCustomers[0].customerId : "";
    let shouldSendMessage = isToVisible ? customerList?.length !== 0 ? true : false : true ;
    if( shouldSendMessage) {
      onSendClick(message, to, customerId);
      handleCancelClick();
    }
  else {
    notification.warning("Cannot send messge. Please invite customer.")
  }
     
  }

  async function getCustomers() {
    const response = await customerApi.getCustomers();
    debugger
    setCustomerList(response);
    if(response.length!==0 ) 
     setTo(response[0].customerId);
    return response;
  }

  function handleCancelClick() {
    setIsMessageModalVisible(false);
    setMessage("");
  }

  function toMessage(event: any) {
    setTo(event.target.value);
  }

  useEffect(() => {
    if (isMessageModalVisible) {
      textareaRef.current?.focus();
    }
  }, [isMessageModalVisible]);

  const handelSearchSelect = (customer: any) => {
    var inputF = document?.getElementById("searchInput") as HTMLInputElement
    if (inputF !== null) {
      inputF.value = customer?.firstName + ' ' + customer?.lastName
    }
  }

  async function handleCustomerSelected(customer: CustomerSearchDto) {
    handelSearchSelect(customer);
    addRecentCustomer(customer);
    setShowCustomer(false);
  }

  return (
    <ConfirmationModal
      isVisible={isMessageModalVisible}
      title="Send message"
      okButtonText="Send Message"
      okDiabled={!hasMessage}
      onOkClick={handleOkClick}
      onCancelClick={handleCancelClick}
    >
      {
        isCustomerVisible === true ?
          <>
            <div style={{ marginBottom: ".5rem" }}>
              <div className="input-group input-group-round">
                <div className="input-group-prepend">
                  <span className="input-group-text">
                    <Icon icon={faSearch} />
                  </span>
                </div>
                <input
                  onChange={debounce((e) => { setSearchText(e.target.value); setShowCustomer(true); },)}
                  ref={searchInputRef}
                  type="search"
                  className="form-control"
                  placeholder="Find a customer"
                  aria-label="Find a customer"
                  id='searchInput'
                />
              </div>
            </div>
            {
              showCustomer === true ?
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
                :
                null
            }
          </>
          :
          null
      }
      <Form.Control
        as="textarea"
        ref={textareaRef}
        value={message}
        rows={10}
        placeholder="Type your message"
        onChange={(e) => { setMessage(e.target.value); }}
      />
      {
        isToVisible === true ?
          <Form.Group>
            <Form.Label> To: </Form.Label>
            <Form.Control as="select" aria-label="To :" onChange={(event) => { toMessage(event) }} data-live-search="true" >

              {
                customerList?.length === 0   ?   
                    <option value="No Customer Found">  No Customer Found </option>
:
                 customerList?.map((item: any) => {
                    return <option value={item.customerId}> {item.fullName} </option>
                  })
                  
              }
            </Form.Control>
          </Form.Group>
          :
          null
      }
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
export default function useSendMessageModal() {
  const [isMessageModalVisible, setIsMessageModalVisible] = useState(false);
  const [isToVisible, setIsToVisible] = useState(false);
  const [isCustomerVisible, setIsCustomerVisible] = useState(false);
  return {
    SendMessageModal,
    isMessageModalVisible,
    isToVisible,
    setIsToVisible,
    setIsMessageModalVisible,
    isCustomerVisible,
    setIsCustomerVisible,
    modalProps: {
      isMessageModalVisible,
      setIsMessageModalVisible,
      isToVisible,
      setIsToVisible,
      isCustomerVisible,
      setIsCustomerVisible
    },
  };
}