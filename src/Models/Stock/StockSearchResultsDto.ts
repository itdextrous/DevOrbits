import SearchOptions from "Common/SearchOptions";
import StockSearchDto from "Models/Stock/StockSearchDto";

export default class StockSearchResultsDto {
  stockList: StockSearchDto[] = [];

  options: SearchOptions = new SearchOptions();
}
