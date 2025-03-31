import BrokerSearchCriteriaDto from "Models/Broker/BrokerSearchCriteriaDto";
import BrokerDto from "Models/Broker/BrokerDto";
import BrokerSearchResultsDto from "Models/Broker/BrokerSearchResultsDto";
import {
  apiGet, apiPost, apiPut, apiDelete, stringify,
} from "./ApiService";

class BrokerApi {
  protected resourceName = `${process.env.REACT_APP_DOMAIN}/api/Admin/Broker`;

  /**
   * Add a Broker
   * @param entity The Broker to add
   */
  add(entity: BrokerDto) {
    return apiPost<BrokerDto, BrokerDto>(this.resourceName, entity);
  }

  /**
   * Modify an existing Broker
   * @param entity The Broker to modify
   */
  update(entity: BrokerDto) {
    return apiPut<BrokerDto, BrokerDto>(`${this.resourceName}/${entity.brokerId}`, entity);
  }

  /**
   * Delete a Broker
   * @param brokerId the id of the Broker to delete
   */
  delete(brokerId: number) {
    return apiDelete<null>(`${this.resourceName}/${brokerId}`);
  }

  /**
   * Find a Broker with the matching brokerId
   * @param brokerId - The id of the Broker to search for
   */
  find(brokerId: string) {
    return apiGet<BrokerDto>(`${this.resourceName}/${brokerId}`);
  }

  /**
   * Search the Broker list using the criteria in the criteria object
   * @param criteria - The criteria object with properties for each search field
   */
  search(criteria: BrokerSearchCriteriaDto) {
    return apiGet<BrokerSearchResultsDto>(`${this.resourceName}/Search`, stringify(criteria));
  }
}

const brokerApi = new BrokerApi();
export default brokerApi;
