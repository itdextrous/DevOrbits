import SavedSearchDto from "Models/SavedSearch/SavedSearchDto";
import { useEffect, useState } from "react";
import { Form } from "react-bootstrap";
import ConfirmationModal from "./ConfirmationModal";

interface Props {
  isVisible: boolean;
  savedSearch: SavedSearchDto;
  onOkClick: (savedSearch: SavedSearchDto) => void;
  onCancelClick: () => void;
}

export default function SaveSearchModal({
  isVisible, savedSearch, onOkClick, onCancelClick,
}: Props) {
  const [search, setSearch] = useState(savedSearch);

  useEffect(() => {
    if (isVisible) {
      // When the modal becomes visible initialize state from the prop
      setSearch(savedSearch);
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isVisible]);

  return (
    <ConfirmationModal
      isVisible={isVisible}
      title="Save Search"
      okButtonText="Save"
      onOkClick={() => { onOkClick(search); }}
      onCancelClick={() => { onCancelClick(); }}
    >
      <Form.Group controlId="description">
        <Form.Label>Description</Form.Label>
        <Form.Control
          type="text"
          className="input"
          autoComplete="off"
          placeholder={savedSearch.description}
          onChange={(e) => setSearch((current) => ({ ...current, description: e.target.value }))}
        />
      </Form.Group>
    </ConfirmationModal>
  );
}
