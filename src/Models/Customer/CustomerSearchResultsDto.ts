import SearchOptions from "Common/SearchOptions";
import CustomerSearchDto from "Models/Customer/CustomerSearchDto";

export default class CustomerSearchResultsDto {
  customerList: CustomerSearchDto[] = [];

  options: SearchOptions = new SearchOptions();
}
