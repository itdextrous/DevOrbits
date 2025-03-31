import CustomerSearchDto from "Models/Customer/CustomerSearchDto";
import CustomerStatus from "Models/Customer/CustomerStatus";

export interface KanbanBoard {
  columns: KanbanColumn[];
}
interface KanbanColumn {
  id: number;
  title: string;
  cards: KanbanCustomerCard[];
}
export class KanbanCustomerCard {
  id: string;

  customer: CustomerSearchDto;

  constructor(customer: CustomerSearchDto) {
    this.id = customer.customerId;
    this.customer = customer;
  }
}
const searchBoardColumn: KanbanColumn = { id: CustomerStatus.Search, title: "Search", cards: [] };
const selectBoardColumn: KanbanColumn = { id: CustomerStatus.Select, title: "Select", cards: [] };
const secureBoardColumn: KanbanColumn = { id: CustomerStatus.Secure, title: "Secure", cards: [] };
const settleBoardColumn: KanbanColumn = { id: CustomerStatus.Settle, title: "Settle", cards: [] };

export interface ColumnsOnBoard {
  10: KanbanColumn;
  20: KanbanColumn;
  30: KanbanColumn;
  40: KanbanColumn;
}

export const columnIndex: ColumnsOnBoard = {
  10: searchBoardColumn,
  20: selectBoardColumn,
  30: secureBoardColumn,
  40: settleBoardColumn,
};

export const initialBoard: KanbanBoard = {
  columns: [
    searchBoardColumn,
    selectBoardColumn,
    secureBoardColumn,
    settleBoardColumn,
  ],
};
