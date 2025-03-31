import * as yup from "yup";
import {
  Formik,
  Form as FormikForm,
  FormikHelpers,
} from "formik";
import { Button } from "react-bootstrap";
import InputText from "Components/Controls/InputText";
import ProfileDto from "../../Models/ProfileDto/ProfileDto";

//eslint-disable-next-line
//const phoneValidateRegex = /^\({0,1}((0|\+61)(2|4|3|7|8)){0,1}\){0,1}(\ |-){0,1}[0-9]{2}(\ |-){0,1}[0-9]{2}(\ |-){0,1}[0-9]{1}(\ |-){0,1}[0-9]{3}$/;
const validationSchema =yup.object().shape({
  email: yup.string().label("Email").required().email().max(100),
  phoneNo: yup.string().required("Phone number is required field").label("Phone").nullable().max(100),
});
const initialValues = {
     email: "",
     phoneNo: ""
   };  

const Profile = () => {
  async function handleFormSubmit(
    formData: ProfileDto,
    { setSubmitting }: FormikHelpers<ProfileDto>
  ) {}
    return (
      <Formik
            initialValues={initialValues}
            validationSchema={validationSchema}
            onSubmit={handleFormSubmit}
        >  
       {({ isSubmitting }) => (
        <FormikForm autoComplete="off">
        <section className="vh-100 login-sec" >
        <div className="container">
          <div className="row d-flex justify-content-center align-items-center h-100">
            <div className="col-md-3">
              <div> 
                <div className="card-body p-2 ">
                  <h4 className="mb-2">Profile </h4> 
                  <div className="form-outline mb-4">
                  {/* <label className="form-label" htmlFor="typeEmailX-2" style={{color:"GrayText"}}>Username</label>
                    <input type="email" id="typeEmailX-2" name="email" className="form-control form-control-lg" />    */}
                    <InputText name="email" label="Username" />
                  </div>
                  <div className="form-outline mb-4">
                    {/* <label htmlFor="phone" style={{color:"GrayText"}}>Phone number</label>
                     <input type="tel" id="phone" name="phone" className="form-control form-control-lg"  /> */}
                     <InputText name="phoneNo" label="Phone number" />
                     {/* pattern="[0-9]{3}-[0-9]{2}-[0-9]{3}" */}
                  </div>
                  <Button
                        type="submit"
                        className="btn btn-primary"
                        variant="primary"
                      >
                        Save
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
export default Profile;