import SearchOptions from "Common/SearchOptions";
import DealerSearchDto from "Models/Dealer/DealerSearchDto";

export default class DealerSearchResultsDto {
  dealerList: DealerSearchDto[] = [];

  options: SearchOptions = new SearchOptions();
}
