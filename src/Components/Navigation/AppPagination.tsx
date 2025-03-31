import SearchOptions from "Common/SearchOptions";
import { Pagination } from "react-bootstrap";

interface Props {
  pagesInfo: SearchOptions | undefined;
  onPageChange: (newPage: number) => void;
}

export default function AppPagination({ pagesInfo, onPageChange }: Props) {
  const {  pageNumber, hasPreviousPage, hasNextPage, totalPages} = pagesInfo || new SearchOptions();
  const page = pageNumber || 1;

  function handlePageChange(newPage: number) {
    window.scrollTo(0, 0);
    onPageChange(newPage);
  }

  return (
    <Pagination>
      <Pagination.First
        onClick={() => { handlePageChange(1); }}
        disabled={page === 1}
      >« First
      </Pagination.First>
      <Pagination.Prev
        onClick={() => { handlePageChange(page - 1); }}
        disabled={!hasPreviousPage}
      >‹ Prev
      </Pagination.Prev>

      <Pagination.Item disabled>Page {page} of {totalPages}</Pagination.Item>

      <Pagination.Next
        onClick={() => { handlePageChange(page + 1); }}
        disabled={!hasNextPage}
      >Next ›
      </Pagination.Next>
      <Pagination.Last
        onClick={() => { handlePageChange(totalPages); }}
        disabled={page === totalPages}
      />
    </Pagination>
  );
}
