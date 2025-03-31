import DealerSearchCriteriaDto from "Models/Dealer/DealerSearchCriteriaDto";
import DealerDto from "Models/Dealer/DealerDto";
import DealerSearchResultsDto from "Models/Dealer/DealerSearchResultsDto";
import {
  apiGet, apiPost, apiPut, apiDelete, stringify,
} from "./ApiService";

class DealerApi {
  protected resourceName = `${process.env.REACT_APP_DOMAIN}/api/Admin/Dealer`;

  /**
   * Add a Dealer
   * @param entity The Dealer to add
   */
  add(entity: DealerDto) {
    return apiPost<DealerDto, DealerDto>(this.resourceName, entity);
  }

  /**
   * Modify an existing Dealer
   * @param entity The Dealer to modify
   */
  update(entity: DealerDto) {
    return apiPut<DealerDto, DealerDto>(`${this.resourceName}/${entity.dealerId}`, entity);
  }

  /**
   * Delete a Dealer
   * @param dealerId the id of the Dealer to delete
   */
  delete(dealerId: number) {
    return apiDelete<null>(`${this.resourceName}/${dealerId}`);
  }

  /**
   * Find a Dealer with the matching dealerId
   * @param dealerId - The id of the Dealer to search for
   */
  find(dealerId: string) {
    return apiGet<DealerDto>(`${this.resourceName}/${dealerId}`);
  }

  /**
   * Search the Dealer list using the criteria in the criteria object
   * @param criteria - The criteria object with properties for each search field
   */
  search(criteria: DealerSearchCriteriaDto) {
    return apiGet<DealerSearchResultsDto>(`${this.resourceName}/Search`, stringify(criteria));
  }
}

const dealerApi = new DealerApi();
export default dealerApi;
