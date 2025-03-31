import SearchOptions from "Common/SearchOptions";

export default class CustomerKanbanCriteria {
  filter: string | null = null;

  options: SearchOptions = new SearchOptions(100, 1);
}
