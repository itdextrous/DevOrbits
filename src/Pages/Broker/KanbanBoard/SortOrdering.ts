import { KanbanBoard } from "./KanbanInterfaces";

export const MinimumSortOrder = -2147483640;
export const MaximumSortOrder = 2147483647;
export const SortOrderNotSet = MaximumSortOrder;

export function calcHalfwaySortOrder(lower: number, upper: number) {
  return Math.round((lower + upper) / 2);
}

export function sortOrderOfCardAbove(updatedBoard: KanbanBoard, columnId: number, position: number) {
  if (position === 0) {
    return MinimumSortOrder;
  }

  const column = findColumn(updatedBoard, columnId);

  const cardAbove = column.cards[position - 1];
  return cardAbove.customer.sortOrder;
}

export function sortOrderOfCardBelow(updatedBoard: KanbanBoard, columnId: number, position: number) {
  const column = findColumn(updatedBoard, columnId);

  if (position >= (column.cards.length - 1)) {
    // No cards in column or position is at the end of the list
    return MaximumSortOrder;
  }

  const cardBelow = column.cards[position + 1];
  return cardBelow.customer.sortOrder;
}

function findColumn(boardToSearch: KanbanBoard, columnId: number) {
  const column = boardToSearch.columns.find((col) => col.id === columnId);

  if (!column) {
    throw new Error(`Unknown column id ${columnId}`);
  }

  return column;
}
