import { NavLink } from "react-router-dom";
const ManageAccount = () => {
    const ManagAccount = [
        {
            changeAccount: "Profile",
            routeManage: "Manage/Profile"
        },
        {
            changeAccount: "Email",
            routeManage: "Manage/Email"
        },
        {
            changeAccount: "Password",
            routeManage: "Manage/ChangePassword"
        }
    ];
    return (
        <section className="vh-100 login-sec" >
            <div className="container">
                <div className="row d-flex justify-content-center align-items-center h-100">
                    <div className="col-md-5">
                        <div>
                            {/* className="card shadow-2-strong" */}
                            <div className="card-body p-2 ">
                                <b> <h1 className="mb-2">Manage your account</h1>
                                </b>
                                <h4>Change your account setting</h4>
                                <hr />
                                <div>
                                    {ManagAccount.map((item, index) => {
                                        return (
                                            <div className="row">
                                                <div className="col-6">
                                                    <div className="list-group" id="list-tab" role="tablist">
                                                        {/* <a className="list-group-item-action primary mt-4" style={{ color: "blue" }} id="list-home-list" data-toggle="list" role="tab" aria-controls="home" href={item.routeManage}>{item.changeAccount}</a> */}
                                                        <NavLink className="list-group-item-action primary mt-4" style={{ color: "blue" }} id="list-home-list" data-toggle="list" role="tab" aria-controls="home" to={item.routeManage}>{item.changeAccount}</NavLink>
                                                        
                                                        {/* <Route render={({ history}) => (
                                                            <button
                                                            type='button'
                                                            onClick={() => { history.push(item.routeManage) }}
                                                            >
                                                           {item.changeAccoutn}
                                                            </button>
                                                        )} /> */}
                                                    </div>
                                                </div>
                                            </div>
                                        )
                                    })}
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </section>
    )
}

export default ManageAccount;