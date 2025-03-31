import BrokerageApplicationSearchCriteriaDto from "Models/BrokerageApplication/BrokerageApplicationSearchCriteriaDto";
import BrokerageApplicationDto from "Models/BrokerageApplication/BrokerageApplicationDto";
import BrokerageApplicationSearchResultsDto from "Models/BrokerageApplication/BrokerageApplicationSearchResultsDto";
import BrokerageDto from "Models/Brokerage/BrokerageDto";
import { apiGet, apiPost, apiPut, apiDelete, stringify } from "./ApiService";

class BrokerageApplicationApi {
  protected resourceNamePublic = `${process.env.REACT_APP_DOMAIN}/api/Public/BrokerageApplication`;
  protected resourceNameAdmin = `${process.env.REACT_APP_DOMAIN}/api/Admin/BrokerageApplication`;

  /**
   * Add a BrokerageApplication
   * @param entity The BrokerageApplication to add
   */
  add(entity: BrokerageApplicationDto) {
    return apiPost<BrokerageApplicationDto, BrokerageApplicationDto>(this.resourceNamePublic, entity);
  }

  /**
   * Modify an existing BrokerageApplication
   * @param entity The BrokerageApplication to modify
   */
  update(entity: BrokerageApplicationDto) {
    return apiPut<BrokerageApplicationDto, BrokerageApplicationDto>(`${this.resourceNameAdmin}/${entity.brokerageApplicationId}`, entity);
  }
  updatePublic(entity: BrokerageApplicationDto) {
    return apiPut<BrokerageApplicationDto, BrokerageApplicationDto>(`${this.resourceNamePublic}/${entity.brokerageApplicationId}`, entity);
  }

  /**
   * Delete a BrokerageApplication
   * @param brokerageApplicationId the id of the BrokerageApplication to delete
   */
  delete(brokerageApplicationId: number) {
    return apiDelete<null>(`${this.resourceNameAdmin}/${brokerageApplicationId}`);
  }

  /**
   * Find a BrokerageApplication with the matching brokerageApplicationId
   * @param brokerageApplicationId - The id of the BrokerageApplication to search for
   */
  find(brokerageApplicationId: string) {
    return apiGet<BrokerageApplicationDto>(`${this.resourceNameAdmin}/${brokerageApplicationId}`);
  }

  /**
   * Search the BrokerageApplication list using the criteria in the criteria object
   * @param criteria - The criteria object with properties for each search field
   */
  search(criteria: BrokerageApplicationSearchCriteriaDto) {
    return apiGet<BrokerageApplicationSearchResultsDto>(`${this.resourceNameAdmin}/Search`, stringify(criteria));
  }

  approve(entity: BrokerageApplicationDto) {
    return apiPost<BrokerageApplicationDto, BrokerageDto>(`${this.resourceNameAdmin}/Approve`, entity);
  }
}

const brokerageApplicationApi = new BrokerageApplicationApi();
export default brokerageApplicationApi;
