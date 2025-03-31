import SavedStockDto from "Models/SavedStock/SavedStockDto";
import StockSearchCriteriaDto from "Models/Stock/StockSearchCriteriaDto";
import StockSearchResultsDto from "Models/Stock/StockSearchResultsDto";
import {
  apiGet, apiPost, apiDelete, stringify,
} from "./ApiService";

class SavedStockApi {
  protected resourceName = `${process.env.REACT_APP_DOMAIN}/api/Customer/SavedStock`;

  /**
   * Save a vehicle to shortlist
   * @param entity The SavedStock to add
   */
  add(entity: SavedStockDto) {
    return apiPost<SavedStockDto, null>(this.resourceName, entity);
  }

  /**
   * Broker adds a vehicle to a customer shortlist
   * @param entity The SavedStock to add
   */
  recommend(entity: SavedStockDto) {
    return apiPost<SavedStockDto, null>(`${process.env.REACT_APP_DOMAIN}/api/Broker/SavedStock`, entity);
  }

  /**
   * Delete a saved vehicle
   * @param stockId the id of the vehicle to delete
   */
  delete(stockId: string) {
    return apiDelete<null>(`${this.resourceName}/${stockId}`);
  }

  /**
   * List saved stock list using the paging criteria in the criteria object
   * @param criteria - The criteria object with properties for paging
   */
 
 list(criteria: StockSearchCriteriaDto) {

    return apiGet<StockSearchResultsDto>(`${process.env.REACT_APP_DOMAIN}/api/Customer/Stock/Saved`, stringify(criteria));
  }
}

const savedStockApi = new SavedStockApi();
export default savedStockApi;
