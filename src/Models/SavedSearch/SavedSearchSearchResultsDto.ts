import SearchOptions from "Common/SearchOptions";
import SavedSearchSearchDto from "Models/SavedSearch/SavedSearchSearchDto";

export default class SavedSearchSearchResultsDto {
  savedSearchList: SavedSearchSearchDto[] = [];

  options: SearchOptions = new SearchOptions();
}
