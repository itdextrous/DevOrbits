import CustomerSearchCriteriaDto from "Models/Customer/CustomerSearchCriteriaDto";
import CustomerDto from "Models/Customer/CustomerDto";
import CustomerSearchResultsDto from "Models/Customer/CustomerSearchResultsDto";
import CustomerKanbanCriteria from "Models/Customer/CustomerKanbanCriteria";
import { apiGet, apiPost, apiPut, apiDelete, stringify } from "./ApiService";
import CustomerRoleDto from 'Models/Customer/CustomerRoleDto';
class CustomerApi {
  protected resourceName = `${process.env.REACT_APP_DOMAIN}/api/Broker/Customer`;

  /**
   * Add a Customer
   * @param entity The Customer to add
   */
  add(entity: CustomerDto) {
    return apiPost<CustomerDto, CustomerDto>(this.resourceName, entity);
  }

  /**
   * Modify an existing Customer
   * @param entity The Customer to modify
   */
  update(entity: CustomerDto) {
    return apiPut<CustomerDto, CustomerDto>(`${this.resourceName}/${entity.customerId}`, entity);
  }

  setStatusAndSort(entity: CustomerDto) {
    return apiPut<CustomerDto, CustomerDto>(`${this.resourceName}/SetStatusAndSort/${entity.customerId}`, entity);
  }

  /**
   * Delete a Customer
   * @param customerId the id of the Customer to delete
   */
  delete(customerId: number) {
    return apiDelete<null>(`${this.resourceName}/${customerId}`);
  }

  /**
   * Find a Customer with the matching customerId
   * @param customerId - The id of the Customer to search for
   */
  find(customerId: string) {
    return apiGet<CustomerDto>(`${this.resourceName}/${customerId}`);
  }

  /**
   * Search the Customer list using the criteria in the criteria object
   * @param criteria - The criteria object with properties for each search field
   */
  search(criteria: CustomerSearchCriteriaDto) {
    return apiGet<CustomerSearchResultsDto>(`${this.resourceName}/Search`, stringify(criteria));
  }

  kanbanList(criteria: CustomerKanbanCriteria) {
    return apiGet<CustomerSearchResultsDto>(`${this.resourceName}/Kanban`, stringify(criteria));
  }
  getCustomers() {
    return apiGet<CustomerRoleDto[]>(`${this.resourceName}/GetAllCustomers`);
  }
}

const customerApi = new CustomerApi();
export default customerApi;
