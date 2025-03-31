import { ReactNode } from "react";
import { Button, Modal } from "react-bootstrap";

interface Props {
  isVisible: boolean;
  title?: string;
  children: ReactNode;
  okButtonText?: string;
  cancelButtonText?: string;
  closeButtonVisible?: boolean;
  centered?: boolean;
  mustRespond?: boolean;
  okDiabled?: boolean;
  onOkClick: () => void;
  onCancelClick: () => void;
}

const defaultProps = {
  title: "Confirm",
  okButtonText: "OK",
  cancelButtonText: "Cancel",
  closeButtonVisible: false,
  centered: true,
  mustRespond: false,
  okDiabled: false,
};

export default function ConfirmationModal({
  isVisible, title, children,
  okButtonText, cancelButtonText,
  closeButtonVisible, centered, mustRespond, okDiabled,
  onOkClick, onCancelClick,
}: Props) {
  return (
    <Modal
      animation={false}
      backdrop={mustRespond ? "static" : true}
      centered={centered}
      show={isVisible}
      onHide={mustRespond ? undefined : onCancelClick}
    >
      <Modal.Header closeButton={closeButtonVisible}>
        <Modal.Title>{title}</Modal.Title>
      </Modal.Header>
      <Modal.Body>{children}</Modal.Body>
      <Modal.Footer>
        <Button
          variant="primary"
          onClick={onOkClick}
          disabled={okDiabled}
        >{okButtonText}
        </Button>
        <Button
          variant="secondary"
          onClick={onCancelClick}
        >{cancelButtonText}
        </Button>
      </Modal.Footer>
    </Modal>
  );
}

ConfirmationModal.defaultProps = defaultProps;
