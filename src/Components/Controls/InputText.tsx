/* eslint-disable react/jsx-props-no-spreading */
import { Form } from "react-bootstrap";
import { useField } from "formik";

interface Props {
  label: string;
  name?: string;
  placeholder?: string;
  disabled?: boolean;
  readOnly?: boolean;
  plaintext?: boolean;
}

const defaultProps = {
  name: "",
  placeholder: "",
  disabled: undefined,
  readOnly: undefined,
  plaintext: undefined,
};

function InputText({
  label, name, placeholder, disabled, readOnly, plaintext, ...props
}: Props) {
  const fieldName = name || label;
  const [field, meta] = useField({ name: fieldName, ...props });

  return (
    <>
      <Form.Group controlId={name || label}>
        <Form.Label>{label || name}</Form.Label>
        <Form.Control
          type="text"
          className="input"
          autoComplete="off"
          placeholder={placeholder}
          disabled={disabled}
          readOnly={readOnly}
          plaintext={plaintext}
          {...props}
          {...field}
        />
        {meta && meta.touched && meta.error ? (
          <Form.Text className="text-error">{meta.error}</Form.Text>
        ) : null}
      </Form.Group>
    </>
  );
}

InputText.defaultProps = defaultProps;

export default InputText;
