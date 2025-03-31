import StockSearchCriteriaDto from "Models/Stock/StockSearchCriteriaDto";
import StockDto from "Models/Stock/StockDto";
import StockSearchResultsDto from "Models/Stock/StockSearchResultsDto";
import DealerStockSearchCriteriaDto from "Models/Stock/DealerStockSearchCriteriaDto";
import { apiGet, apiPost, stringify } from "./ApiService";

class StockApi {
  protected resourceName = `${process.env.REACT_APP_DOMAIN}/api/`;
  /**
   * Find Stock with the matching stockId
   * @param stockId - The id of the Stock to search for
   */

  find(stockId: string) {
    return apiGet<StockDto>(`${this.resourceName}Stock/${stockId}`);
  }

  /**
   * Search the Stock list using the criteria in the criteria object
   * @param criteria - The criteria object with properties for each search field
   */
  search(criteria: StockSearchCriteriaDto) {
    return apiGet<StockSearchResultsDto>(`${this.resourceName}Stock/Search`, stringify(criteria));
  }

  dealerSearch(criteria: DealerStockSearchCriteriaDto) {
    return apiGet<StockSearchResultsDto>(`${this.resourceName}Dealer/Stock/Search`, stringify(criteria));
  }

  makes() {
    return apiGet<StockSearchResultsDto>(`${this.resourceName}Stock/Makes`, stringify({}));
  }

  models(make: string) {
    return apiGet<StockSearchResultsDto>(`${this.resourceName}Stock/Models/${make}`);
  }

  bodyTypes(make: string, model: string) {
    return apiGet<StockSearchResultsDto>(`${this.resourceName}Stock/BodyTypes`, stringify({ make, model }));
  }

  uploadStocks(stockList: StockDto[]) {
    return apiPost<StockDto[], StockDto>(`${this.resourceName}Stock/CreateStocks`, stockList);
  }
}

const stockApi = new StockApi();
export default stockApi;
