import DealerApplicationSearchCriteriaDto from "Models/DealerApplication/DealerApplicationSearchCriteriaDto";
import DealerApplicationDto from "Models/DealerApplication/DealerApplicationDto";
import DealerApplicationSearchResultsDto from "Models/DealerApplication/DealerApplicationSearchResultsDto";
import DealerDto from "Models/Dealer/DealerDto";
import {
  apiGet, apiPost, apiPut, apiDelete, stringify,
} from "./ApiService";

class DealerApplicationApi {
  protected resourceName = `${process.env.REACT_APP_DOMAIN}/api/Admin/DealerApplication`;
  protected resourceNamePublic = `${process.env.REACT_APP_DOMAIN}/api/Public/DealerApplication`;
 
  /**
   * Add a DealerApplication
   * @param entity The DealerApplication to add
   */
  add(entity: DealerApplicationDto) {
    return apiPost<DealerApplicationDto, DealerApplicationDto>(`${this.resourceNamePublic}`, entity);
  }

  /**
   * Modify an existing DealerApplication
   * @param entity The DealerApplication to modify
   */
  update(entity: DealerApplicationDto) {
    return apiPut<DealerApplicationDto, DealerApplicationDto>(`${this.resourceName}/${entity.dealerApplicationId}`, entity);
  }

  /**
   * Delete a DealerApplication
   * @param dealerApplicationId the id of the DealerApplication to delete
   */
  delete(dealerApplicationId: number) {
    return apiDelete<null>(`${this.resourceName}/${dealerApplicationId}`);
  }

  /**
   * Find a DealerApplication with the matching dealerApplicationId
   * @param dealerApplicationId - The id of the DealerApplication to search for
   */
  find(dealerApplicationId: string) {
    return apiGet<DealerApplicationDto>(`${this.resourceName}/${dealerApplicationId}`);
  }

  /**
   * Search the DealerApplication list using the criteria in the criteria object
   * @param criteria - The criteria object with properties for each search field
   */
  search(criteria: DealerApplicationSearchCriteriaDto) {
    return apiGet<DealerApplicationSearchResultsDto>(`${this.resourceName}/Search`, stringify(criteria));
  }

  approve(entity: DealerApplicationDto) {
    return apiPost<DealerApplicationDto, DealerDto>(`${this.resourceName}/Approve`, entity);
  }
  
  updatePublic(entity: DealerApplicationDto) {
    return apiPut<DealerApplicationDto, DealerApplicationDto>(`${this.resourceNamePublic}/${entity.dealerApplicationId}`, entity);
  }
}

const dealerApplicationApi = new DealerApplicationApi();
export default dealerApplicationApi;
