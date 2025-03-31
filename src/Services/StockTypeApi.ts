import StockTypeResultDto from "Models/StockType/StockTypeResultDto";
import { apiGet, stringify } from "./ApiService";

class StockTypeApi {
  protected resourceName = `${process.env.REACT_APP_DOMAIN}/api/StockType`;

  getStockType(criteria: StockTypeResultDto | null) {
    return apiGet<StockTypeResultDto>(`${this.resourceName}/GetStockType`, stringify(criteria));
  }
}

const stockTypeApi = new StockTypeApi();
export default stockTypeApi;
