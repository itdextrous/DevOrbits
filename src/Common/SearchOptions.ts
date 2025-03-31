export default class SearchOptions {
  pageSize: number | null = null;

  pageNumber: number | null = null;

  sortBy = "";

  sortAscending = true;

  totalPages = 0;

  totalItems = 0;

  hasPreviousPage = false;

  hasNextPage = false;

  constructor(pageSize: number | null = null, pageNumber: number | null = null) {
    this.pageSize = pageSize;
    this.pageNumber = pageNumber;
  }

  setSortDirection(direction: string) {
    this.sortAscending = direction === "asc";
  }
}
