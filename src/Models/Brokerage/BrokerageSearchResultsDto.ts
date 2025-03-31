import SearchOptions from "Common/SearchOptions";
import BrokerageSearchDto from "Models/Brokerage/BrokerageSearchDto";

export default class BrokerageSearchResultsDto {
  brokerageList: BrokerageSearchDto[] = [];

  options: SearchOptions = new SearchOptions();
}
