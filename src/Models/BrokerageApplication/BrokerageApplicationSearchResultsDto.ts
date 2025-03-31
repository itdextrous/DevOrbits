import SearchOptions from "Common/SearchOptions";
import BrokerageApplicationSearchDto from "Models/BrokerageApplication/BrokerageApplicationSearchDto";

export default class BrokerageApplicationSearchResultsDto {
  brokerageApplicationList: BrokerageApplicationSearchDto[] = [];

  options: SearchOptions = new SearchOptions();
}
