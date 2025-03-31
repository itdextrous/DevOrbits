import BrokerageSearchCriteriaDto from "Models/Brokerage/BrokerageSearchCriteriaDto";
import BrokerageDto from "Models/Brokerage/BrokerageDto";
import BrokerageSearchResultsDto from "Models/Brokerage/BrokerageSearchResultsDto";
import {
  apiGet, apiPost, apiPut, apiDelete, stringify,
} from "./ApiService";

class BrokerageApi {
  protected resourceName = `${process.env.REACT_APP_DOMAIN}/api/Admin/Brokerage`;

  /**
   * Add a Brokerage
   * @param entity The Brokerage to add
   */
  add(entity: BrokerageDto) {
    return apiPost<BrokerageDto, BrokerageDto>(this.resourceName, entity);
  }

  /**
   * Modify an existing Brokerage
   * @param entity The Brokerage to modify
   */
  update(entity: BrokerageDto) {
    return apiPut<BrokerageDto, BrokerageDto>(`${this.resourceName}/${entity.brokerageId}`, entity);
  }

  /**
   * Delete a Brokerage
   * @param brokerageId the id of the Brokerage to delete
   */
  delete(brokerageId: number) {
    return apiDelete<null>(`${this.resourceName}/${brokerageId}`);
  }

  /**
   * Find a Brokerage with the matching brokerageId
   * @param brokerageId - The id of the Brokerage to search for
   */
  find(brokerageId: string) {
    return apiGet<BrokerageDto>(`${this.resourceName}/${brokerageId}`);
  }

  /**
   * Search the Brokerage list using the criteria in the criteria object
   * @param criteria - The criteria object with properties for each search field
   */
  search(criteria: BrokerageSearchCriteriaDto) {
    return apiGet<BrokerageSearchResultsDto>(`${this.resourceName}/Search`, stringify(criteria));
  }
}

const brokerageApi = new BrokerageApi();
export default brokerageApi;
