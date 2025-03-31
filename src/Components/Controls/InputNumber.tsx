/* eslint-disable react/jsx-props-no-spreading */
import { Form } from "react-bootstrap";
import { useField } from "formik";

interface Props {
  label: string;
  name?: string;
  allowDecimals?: boolean;
}

const defaultProps = {
  name: "",
  allowDecimals: false,
};

function InputNumber({
  label, name, allowDecimals, ...props
}: Props) {
  const [field, meta] = useField({ name: name || label, ...props });

  return (
    <>
      <Form.Group controlId={name || label}>
        <Form.Label>{label || name}</Form.Label>
        <Form.Control
          type="number"
          step={allowDecimals ? "any" : "1"}
          className="input"
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

InputNumber.defaultProps = defaultProps;

export default InputNumber;
