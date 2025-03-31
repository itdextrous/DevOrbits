import { Col, Row } from "react-bootstrap";
import savedSearchApi from "Services/SavedSearchApi";
import SavedSearchSearchCriteriaDto from "Models/SavedSearch/SavedSearchSearchCriteriaDto";
import { useQuery } from "react-query";
import Icon from "Components/Icon";
import { faSearch, faTrash } from "@fortawesome/free-solid-svg-icons";
import { Link } from "react-router-dom";
import SavedSearchSearchDto from "Models/SavedSearch/SavedSearchSearchDto";
import Qs from "qs";
import { useState } from "react";
import ConfirmationModal from "Components/Modals/ConfirmationModal";
import generateSavedSearchDescription from "./SavedSearch";

export default function SavedSearchList() {
  const criteria = new SavedSearchSearchCriteriaDto();
  const [isDeleteModalVisible, setIsDeleteModalVisible] = useState(false);
  const [searchToDelete, setSearchToDelete] = useState<SavedSearchSearchDto | null>(null);

  const query = useQuery(["SavedSearchList", criteria], () => savedSearchApi.list(criteria));

  async function handleDeleteConfirmed() {
    if (!searchToDelete) { return; }

    setIsDeleteModalVisible(false);
    await savedSearchApi.delete(searchToDelete.savedSearchId);
    await query.refetch();
  }

  function getQueryString(search: SavedSearchSearchDto) {
    // Keep the query string clean with just the properties that we want
    const searchProperties = {
      stockType: search.stockType,
      make: search.make,
      model: search.model,
      bodyType: search.bodyType,
      priceMin: search.priceMin,
      priceMax: search.priceMax,
      location: search.location,
      keywords: search.keywords ? search.keywords : null,
    };
    return Qs.stringify(searchProperties, { addQueryPrefix: true, skipNulls: true });
  }

  return (
    <>
      <h2>Saved Searches</h2>
      <Row>
        {query.data?.savedSearchList !== undefined ?
          query.data && query.data?.savedSearchList?.map((search) => (
            <Col
              key={search.savedSearchId}
              md={6}
              sm={12}
            >
              <div className="card card-project">
                <div className="card-body">
                  <div className="card-title">
                    <Link to={`/customer/vehicle/search${getQueryString(search)}`}>
                      <h5><Icon icon={faSearch} /> {search.description}</h5>
                    </Link>
                  </div>
                  <div className="card-meta d-flex justify-content-between">
                    <div className="d-flex align-items-center">
                      <span className="text-small">Search for {generateSavedSearchDescription(search)}</span>
                    </div>
                    <span className="text-small">
                      <Icon
                        icon={faTrash}
                        onClick={() => {
                          setSearchToDelete(search);
                          setIsDeleteModalVisible(true);
                        }}
                      />
                    </span>
                  </div>
                </div>
              </div>
            </Col>)) : null}
      </Row>

      <ConfirmationModal
        isVisible={isDeleteModalVisible}
        title="Confirm Delete"
        okButtonText="Delete"
        onOkClick={async () => { await handleDeleteConfirmed(); }}
        onCancelClick={() => { setIsDeleteModalVisible(false); }}
      >
        Are you sure you want to delete this saved search?
      </ConfirmationModal>
    </>
  );
}
