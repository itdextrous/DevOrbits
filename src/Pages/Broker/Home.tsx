/* eslint-disable jsx-a11y/control-has-associated-label */
import Board, { moveCard } from "@lourenci/react-kanban";
import "@lourenci/react-kanban/dist/styles.css";
import Icon from "Components/Icon";
import { debounce } from "lodash";
import CustomerKanbanCriteria from "Models/Customer/CustomerKanbanCriteria";
import React, { useEffect, useRef, useState } from "react";
import { Button, Col, Container, FormControl, InputGroup, Row } from "react-bootstrap";
import { Link, useHistory } from "react-router-dom";
import customerApi from "Services/CustomerApi";
import { initialBoard, columnIndex, ColumnsOnBoard, KanbanCustomerCard } from "./KanbanBoard/KanbanInterfaces";
import KanbanCard from "./KanbanBoard/KanbanCard";
import { calcHalfwaySortOrder, MinimumSortOrder, MaximumSortOrder, SortOrderNotSet, sortOrderOfCardAbove, sortOrderOfCardBelow } from "./KanbanBoard/SortOrdering";
import { useRecentCustomers } from "./RecentCustomerContext";
import CustomerSearchDto from "Models/Customer/CustomerSearchDto";

export default function Home() {
  const history = useHistory();
  const { addRecentCustomer } = useRecentCustomers();
  const [board, setBoard] = useState(initialBoard);
  const [customerCount, setCustomerCount] = useState(0);
  const [budget, setBudget] = useState(0);
  const [searchText, setSearchText] = useState("");
  const searchTextRef = useRef<HTMLInputElement | null>(null);
  const [customerList, setCustomerList] = useState<CustomerSearchDto[] | null>(null);

  useEffect(() => {
    async function getCustomerList() {
      const query = new CustomerKanbanCriteria();
      query.filter = searchText;
      const response = await customerApi.kanbanList(query);
      return response?.customerList;
    }

    function createBoardState(customerList: CustomerSearchDto[]) {
      // reset board state
      initialBoard.columns.forEach((boardColumn) => {
        const currentColumn = boardColumn;
        currentColumn.cards = [];
      });

      let previousStatus = 0;
      let firstCustomerInColumn: CustomerSearchDto | null = null;
      let previousCustomerInColumn: CustomerSearchDto | null = null;
      let budget = 0;
      customerList?.forEach(async (currentCustomer) => {
        // put into correct columns by status
        const customer = currentCustomer;
        const columnForStatus = columnIndex[customer.status as keyof ColumnsOnBoard];
        columnForStatus.cards.push(new KanbanCustomerCard(customer));

        if (customer?.status !== previousStatus) {
          // Different status so move to next column
          firstCustomerInColumn = customer;
          previousCustomerInColumn = null;
        }

        if (customer.sortOrder === SortOrderNotSet) {
          // New customer - Calculate initial sort order
          if (firstCustomerInColumn && firstCustomerInColumn === customer) {
            // This card is the first in its column
            customer.sortOrder = MinimumSortOrder + MaximumSortOrder;
          } else if (previousCustomerInColumn) {
            // Add to the end of the list
            customer.sortOrder = calcHalfwaySortOrder(previousCustomerInColumn.sortOrder, MaximumSortOrder);
          }

          await customerApi.setStatusAndSort(customer);
        }

        previousStatus = customer.status;
        previousCustomerInColumn = customer;
        budget = budget + Number(currentCustomer.budget);
        setBudget(budget);
      });
      
      // Sort the cards according to the sort order values
      initialBoard.columns.forEach((column) => column.cards.sort((a, b) => a.customer.sortOrder - b.customer.sortOrder));
      setCustomerList(customerList);
      setCustomerCount(customerList?.length);
      setBoard({ ...initialBoard });
    }

    getCustomerList()
      .then((results) => {
        createBoardState(results);
      });
  }, [searchText]);

  async function handleCardDragEnd(
    movedCard: KanbanCustomerCard,
    source: { fromColumnId: number; fromPosition: number; },
    destination: { toColumnId: number; toPosition: number; },
  ) {
    const { customer } = movedCard;

    const updatedBoard = moveCard(board, source, destination);

    customer.status = destination.toColumnId;

    const above = sortOrderOfCardAbove(updatedBoard, destination.toColumnId, destination.toPosition);
    const below = sortOrderOfCardBelow(updatedBoard, destination.toColumnId, destination.toPosition);
    customer.sortOrder = calcHalfwaySortOrder(above, below);

    setBoard(updatedBoard);
    await customerApi.setStatusAndSort(customer);
    addRecentCustomer(customer);
  }

  function handleSearchTextChange(e: React.ChangeEvent<HTMLInputElement>) {
    setSearchText(String(e.target.value));
  }

  return (
    <>
      <Container>
        <Row>
          <Col md="auto">
            <Link to="/broker/customer/invite" className="btn btn-primary">
              <Icon icon="plus" /> Customer
            </Link>
          </Col>
          <Col md="auto">
            <InputGroup className="mb-3">
              <FormControl
                defaultValue={searchText}
                placeholder="Customer search"
                onChange={debounce(handleSearchTextChange, 500)}
                ref={searchTextRef}


                aria-label="Customer search"
              />
              <InputGroup.Append>
                <Button
                  variant="outline-primary"
                  onClick={() => {
                    if (searchTextRef?.current) {
                      searchTextRef.current.focus();
                      searchTextRef.current.value = "";
                    }
                    setSearchText("");
                  }}
                ><Icon icon="times-circle" />
                </Button>
              </InputGroup.Append>
            </InputGroup>
          </Col>
          <Col className="text-right">${budget} - {customerCount} customers</Col>
        </Row>
      </Container>
      {
        customerList !== null ? customerList?.length !== 0
          ?
          <Board
            disableColumnDrag
            onCardDragEnd={handleCardDragEnd}
            renderCard={(card: KanbanCustomerCard) => (
              <KanbanCard
                customer={card.customer}
                onClick={(customer) => { history.push(`/broker/customer/edit/${customer.customerId}`); }}
              />

            )
            }
          >

            {board}
          </Board>
          :
          <Board>{board}</Board>
          :
          null
      }
    </>
  );
}
