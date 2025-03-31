import * as yup from "yup";
import { Formik, Form as FormikForm, FormikHelpers } from "formik";
import InviteCustomerDto from "../../Models/InviteCustomer/InviteCustomerDto";
import { Button } from "react-bootstrap";
import InputText from "Components/Controls/InputText";
const validationSchema = yup.object().shape({
  inviteCode: yup.string().min(6, "code must be 6 digit"),
  password: yup.string().min(8, "password must be 8 character").required(),
  confirmPassword: yup.string().min(8)
});

const initialValues = {
  inviteCode: "",
  password: "",
  confirmPassword: "",
};

const InviteCustomer = () => {
  async function handleFormSubmit(
    formData: InviteCustomerDto,
    { setSubmitting }: FormikHelpers<InviteCustomerDto>
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
                <div className="col-md-6">
                  <div className="card shadow-2-strong">
                    <div className="card-body p-4 ">
                      <b>
                        {" "}
                        <h3 className="mb-4">
                          Log in with your invitation code
                        </h3>
                        <p style={{ color: "GrayText" }}>
                          Login and activate your account with your invitation
                          code.
                        </p>
                      </b>
                      <hr />
                      <div className="form-outline mb-4">
                     
                        <InputText name="inviteCode" label="Invite Code" />
                      </div>
                      <div className="form-outline mb-4">
                      
                        <InputText name="password" label="New Password" />
                      </div>
                      <div className="form-outline mb-4">
                       
                        <InputText
                          name="confirmPassword"
                          label="Confirm Password"
                        />
                      </div>
                  
                      <Button
                        type="submit"
                        className="btn btn-primary"
                        variant="primary"
                      >
                        Save
                      </Button>
                      <Button
                        type="submit"
                        className="btn btn-light"
                        variant="primary"
                      >
                        Cancel
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
export default InviteCustomer;
