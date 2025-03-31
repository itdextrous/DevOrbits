import * as yup from "yup";
import {
  Formik,
  Form as FormikForm,
  FormikHelpers,
  Field,
  ErrorMessage,
} from "formik";
import LoginDto from "../../Models/Login/LoginDto";
// import { Button } from "react-bootstrap";
import InputText from "Components/Controls/InputText";

const validationSchema = yup.object().shape({
  email: yup
    .string()
    .label("Email")
    .required("email is required field")
    .email()
    .max(100),
  password: yup.string().min(4, "password must be 4 character").required(),
  acceptTerms: yup
    .bool()
    .oneOf([true], "Accept Terms is required"),
});

const initialValues = {
  email: "",
  password: "",
  acceptTerms: false,
};
export default function Login() {
  async function handleFormSubmit(
    formData: LoginDto,
    { setSubmitting }: FormikHelpers<LoginDto>
  ) {}
  return (
    <Formik
      initialValues={initialValues}
      validationSchema={validationSchema}
      onSubmit={handleFormSubmit}
    >
      {({ isSubmitting, errors, touched }) => (
        <FormikForm autoComplete="off">
          <section className="vh-100 login-sec">
            <div className="container">
              <div className="row d-flex justify-content-center align-items-center h-100">
                <div className="col-md-4">
                  <div>
                    <div className="card-body p-2">
                      <b>
                        {" "}
                        <h1 className="mb-2">Log in </h1>
                      </b>
                      <h4>Use a local account to log in.</h4>
                      <hr />

                      <div className="form-outline mb-2">
                        <InputText name="email" label="Email" />
                      </div>

                      <div className="form-outline mb-2">
                        <InputText name="password" label="Password" />
                      </div>

                      <div className="form-group form-check">
                        <Field
                          type="checkbox"
                          name="acceptTerms"
                          className={
                            "form-check-input " +
                            (errors.acceptTerms && touched.acceptTerms
                              ? " is-invalid"
                              : "")
                          }
                        />
                        <label
                          htmlFor="acceptTerms"
                          className="form-check-label"
                        >
                          {" "}
                          keep me signed in
                        </label>
                        <ErrorMessage
                          name="acceptTerms"
                          component="div"
                          className="invalid-feedback"
                        />
                      </div>

                      {/* <Button
                        type="submit"
                        className="btn btn-primary"
                        variant="primary"
                      >
                        Log in
                      </Button> */}
                      <div className="form-group">
                        <p className="mt-4">
                          <a href="/ForgotPassword">Forgot your password?</a>
                        </p>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </section>
        </FormikForm>
      )}
    </Formik>
  );
}




// import React, { useState } from "react";

// import Form from "react-bootstrap/Form";

// import Button from "react-bootstrap/Button";

// import "./Login.css";

// export default function Login() {

//   const [email, setEmail] = useState("");

//   const [password, setPassword] = useState("");

//   function validateForm() {

//     return email.length > 0 && password.length > 0;

//   }

//   function handleSubmit(event) {

//     event.preventDefault();

//   }

//   return (

//     <div className="Login">

//       <Form onSubmit={handleSubmit}>

//         <Form.Group size="lg" controlId="email">

//           <Form.Label>Email</Form.Label>

//           <Form.Control

//             autoFocus

//             type="email"

//             value={email}

//             onChange={(e) => setEmail(e.target.value)}

//           />

//         </Form.Group>

//         <Form.Group size="lg" controlId="password">

//           <Form.Label>Password</Form.Label>

//           <Form.Control

//             type="password"

//             value={password}

//             onChange={(e) => setPassword(e.target.value)}

//           />

//         </Form.Group>

//         <Button block size="lg" type="submit" disabled={!validateForm()}>

//           Login

//         </Button>

//       </Form>

//     </div>

//   );

// }