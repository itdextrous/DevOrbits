import SearchOptions from "Common/SearchOptions";
import DealerApplicationSearchDto from "Models/DealerApplication/DealerApplicationSearchDto";

export default class DealerApplicationSearchResultsDto {
  dealerApplicationList: DealerApplicationSearchDto[] = [];

  options: SearchOptions = new SearchOptions();
}
