/* eslint-disable react/jsx-props-no-spreading */
import { Form } from "react-bootstrap";
import { useField } from "formik";

interface Props {
  label: string;
  name?: string;
  rows?: number;
}

const defaultProps = {
  name: "",
  rows: 3,
};

function InputTextArea({
  label, name, rows, ...props
}: Props) {
  const [field, meta] = useField({ name: name || label, ...props });

  return (
    <>
      <Form.Group controlId={name || label}>
        <Form.Label>{label || name}</Form.Label>
        <Form.Control
          as="textarea"
          className="input"
          rows={rows}
          autoComplete="off"
          {...props}
          {...field}
        />
        {(meta && meta.touched && meta.error) && (
          <Form.Text className="text-error">{meta.error}</Form.Text>
        )}
      </Form.Group>
    </>
  );
}

InputTextArea.defaultProps = defaultProps;

export default InputTextArea;
