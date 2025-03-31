import SearchOptions from "Common/SearchOptions";

export default class DealerStockSearchCriteriaDto {
  searchText: string = "";

  options: SearchOptions = new SearchOptions(10, 1);
}
