import SavedSearchSearchCriteriaDto from "Models/SavedSearch/SavedSearchSearchCriteriaDto";
import SavedSearchDto from "Models/SavedSearch/SavedSearchDto";
import SavedSearchSearchResultsDto from "Models/SavedSearch/SavedSearchSearchResultsDto";
import {
  apiGet, apiPost, apiDelete, stringify,
} from "./ApiService";

class SavedSearchApi {
  protected resourceName = `${process.env.REACT_APP_DOMAIN}/api/Customer/SavedSearch`;

  /**
   * Add a SavedSearch
   * @param entity The SavedSearch to add
   */
  add(entity: SavedSearchDto) {
    return apiPost<SavedSearchDto, null>(this.resourceName, entity);
  }

 
  brokerAdd(entity: SavedSearchDto) {
    return apiPost<SavedSearchDto, null>(`${process.env.REACT_APP_DOMAIN}/api/Broker/SavedSearch`, entity);
  }


  /**
   * Delete a SavedSearch
   * @param savedSearchId the id of the SavedSearch to delete
   */
  delete(savedSearchId: string) {
    return apiDelete<null>(`${this.resourceName}/${savedSearchId}`);
  }

  /**
   * Search the SavedSearch list using the criteria in the criteria object
   * @param criteria - The criteria object with properties for each search field
   */
  list(criteria: SavedSearchSearchCriteriaDto) {
    return apiGet<SavedSearchSearchResultsDto>(`${this.resourceName}/List`, stringify(criteria));
  }
}

const savedSearchApi = new SavedSearchApi();
export default savedSearchApi;
