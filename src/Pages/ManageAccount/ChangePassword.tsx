import * as yup from "yup";
import { Formik, Form as FormikForm } from "formik";
import InputText from "Components/Controls/InputText";
import { Button } from "react-bootstrap";

const validationSchema = yup.object().shape({
  password: yup.string().label("Password").required().min(10),
  newPassword:yup.string().label("New Password").required().min(10),
  confrimPassword:yup.string().label("confirm password").required().min(10)
});
const initialValues = {
  password: "",
  newPassword:"",
  confrimPassword:""
};
const ChangePassword = () => {
  const handleFormSubmit = (e: any) => {
    console.log(e)
  }
  return (
    <Formik
      initialValues={initialValues}
      validationSchema={validationSchema}
      onSubmit={handleFormSubmit}
    >
     {({ isSubmitting, errors, touched }) => (
        <FormikForm autoComplete="off">
        <section className="vh-100 login-sec" >
            <div className="container">
              <div className="row d-flex justify-content-center align-items-center h-100">
                <div className="col-md-4">
                  <div>
                  
                    <div className="card-body p-2 ">
                      <h4 className="mb-2">Change password</h4>
                      <div className="form-outline mb-4">
                       
                        <InputText name="password" label="Current password" />
                      </div>
                      <div className="form-outline mb-4">
                      
                        <InputText name="newPassword" label="New password" />
                      </div>
                      <div className="form-outline mb-4">
                      <InputText name="confrimPassword" label="Confirm new password" />
                      </div>
                      <Button
                        type="submit"
                        className="btn btn-primary"
                        variant="primary"
                      >
                        Update password
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
  )
}

export default ChangePassword;