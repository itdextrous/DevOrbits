import * as yup from "yup";
import { Formik, Form as FormikForm, FormikHelpers } from "formik";
import { Button } from "react-bootstrap";
import InputText from "Components/Controls/InputText";
import ForgotPasswordDto from "../../Models/ForgotPassword/ForgotPasswordDto";

const validationSchema = yup.object().shape({
  email: yup
    .string()
    .label("Email")
    .required("email is required field")
    .email()
    .max(100),
});
const initialValues = {
  email: "",
};

const ForgotPassword = () => {
  async function handleFormSubmit(
    formData: ForgotPasswordDto,
    { setSubmitting }: FormikHelpers<ForgotPasswordDto>
  ) {}

  return (
    <Formik
      initialValues={initialValues}
      validationSchema={validationSchema}
      onSubmit={handleFormSubmit}
    >
      {({ isSubmitting }) => (
        <FormikForm autoComplete="off">
          <section className="vh-100 login-sec">
            <div className="container">
              <div className="row d-flex justify-content-center align-items-center h-100">
                <div className="col-md-5">
                  <div>
                    <div className="card-body p-2 ">
                      <b>
                        {" "}
                        <h1 className="mb-2">Forgot your password?</h1>
                      </b>
                      <h4>Enter your email</h4>
                      <hr />
                      <div className="form-outline mb-4">
                        <InputText name="email" label="Email" />
                      </div>
                      <Button
                        type="submit"
                        className="btn btn-primary"
                        variant="primary"
                      >
                        Submit
                      </Button>
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
};

export default ForgotPassword;
