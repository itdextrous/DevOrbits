import * as yup from "yup";
import { Formik,Form as FormikForm} from "formik";
import { Button } from "react-bootstrap";
import InputText from "Components/Controls/InputText";


const validationSchema =yup.object().shape({
  email: yup.string().label("Email").required().email().max(100),
  newEmail:yup.string().label("New email").required().max(100)
});
const initialValues = {
     email: "",
     newEmail:""
   };


const Email = () => {

  const handleFormSubmit = (e:any) => {
    console.log(e)
 }
    return (
      <Formik 
      validationSchema={validationSchema}
      initialValues={initialValues}
      onSubmit={handleFormSubmit}
      >
         {({ isSubmitting, errors, touched }) => (
        <FormikForm autoComplete="off">
        <section className="vh-100 login-sec">
        <div className="container">
          <div className="row d-flex justify-content-center align-items-center h-100">
            <div className="col-md-4">
              <div> 
          
                <div className="card-body p-2 ">
                  <h4 className="mb-2">Manage Email </h4> 
                  <div className="form-outline mb-4">
               
                    <InputText name="email" label="Email" />
                  </div>
                  <div className="form-outline mb-4">
                
                    <InputText name="newEmail" label="New Email" />
                  </div>
                  <Button
                        type="submit"
                        className="btn btn-primary"
                        variant="primary"
                      >
                        Change email
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

export default Email;