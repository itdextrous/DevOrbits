import React from "react";
import Roles from "Common/Authorization/Roles";
import adminRoutes from "Pages/Admin/RoutesForAdmin";
import publicRoutes from "Pages/Public/RoutesForPublic";
import brokerRoutes from "Pages/Broker/RoutesForBroker";
import dealerRoutes from "Pages/Dealer/RoutesForDealer";
import customerRoutes from "Pages/Customer/RoutesForCustomer";

export interface RoleRoute {
  path: string,
  requiresAuth?: boolean;
  requiredRole?: string,
  routes?: MenuRoute[];
}

export interface MenuRoute extends RoleRoute {
  title: string;
  menu: boolean;
  menuPath?: string,
  // eslint-disable-next-line no-undef
  component: any;
  img: any;
}

export const appRoutes: MenuRoute[] = [
  { // Admin Home
    title: "Home",
    path: "/",
    menu: true,
    requiresAuth: true,
    requiredRole: Roles.Administrator,
    routes: adminRoutes,
    component: React.lazy(() => import(/* webpackChunkName: "admin" */ "Pages/Admin/Home")),
    img:"",
  },
  { // Broker Home
    title: "Home",
    path: "/",
    menu: true,
    requiresAuth: true,
    requiredRole: Roles.Broker,
    routes: brokerRoutes,
    component: React.lazy(() => import(/* webpackChunkName: "broker" */ "Pages/Broker/Home")),
    img:"",
  },
  { // Dealer Home
    title: "Home",
    path: "/",
    menu: true,
    requiresAuth: true,
    requiredRole: Roles.Dealer,
    routes: dealerRoutes,
    component: React.lazy(() => import(/* webpackChunkName: "dealer" */ "Pages/Dealer/Home")),
    img:"",
  },
  { // Customer Home
    title: "Home",
    path: "/",
    menu: true,
    requiresAuth: true,
    requiredRole: Roles.Customer,
    routes: customerRoutes,
    component: React.lazy(() => import(/* webpackChunkName: "customer" */ "Pages/Customer/Vehicle/Search")),
    img:"",
  },

 
  { // Public Home
    title: "Home",
    path: "/",
    menu: true,
    requiresAuth: false,
    routes: publicRoutes,
    component: React.lazy(() => import(/* webpackChunkName: "public" */ "Pages/Public/Home")),
    img:"",
  },
  
    // ////////////////////////////////////// ////////////// //////
    // { // Login Home
    //   title: "Home",
    //   path: "/Login",
    //   menu: true,
    //   requiresAuth: false,
    //   routes: customerRoutes,
    //   component: React.lazy(() => import(/* webpackChunkName: "Login" */ "Pages/LoginPage/Login")),
    //   img:"",
    // },
    // { // Invite Customer 
    //   title: "Home",
    //   path: "/InviteCustomer",
    //   menu: true,
    //   requiresAuth: false,
    //   routes: customerRoutes,
    //   component: React.lazy(() => import(/* webpackChunkName: "InviteCustomer" */ "Pages/InviteCustomers/InviteCutomer")),
    //   img:"",
    // },
    // { // Forgot password 
    //   title: "Home",
    //   path: "/ForgotPassword",
    //   menu: true,
    //   requiresAuth: false,
    //   routes: customerRoutes,
    //   component: React.lazy(() => import(/* webpackChunkName: "InviteCustomer" */ "Pages/LoginPage/ForgotPassword")),
    //   img:"",
    // },
  
    // { // Manage Profile 
    //   title: "Home",
    //   path: "/Manage",
    //   menu: true,
    //   requiresAuth: false,
    //   routes: customerRoutes,
    //   component: React.lazy(() => import(/* webpackChunkName: "InviteCustomer" */ "Pages/ManageAccount/Manage")),
    //   img:"",
    // },
    // { // Manage Profile 
    //   title: "Home",
    //   path: "/Manage/Profile",
    //   menu: true,
    //   requiresAuth: false,
    //   routes: customerRoutes,
    //   component: React.lazy(() => import(/* webpackChunkName: "InviteCustomer" */ "Pages/ManageAccount/Profile")),
    //   img:"",
    // },
  
    // { // Manage Email 
    //   title: "Home",
    //   path: "/Manage/Email",
    //   menu: true,
    //   requiresAuth: false,
    //   routes: customerRoutes,
    //   component: React.lazy(() => import(/* webpackChunkName: "InviteCustomer" */ "Pages/ManageAccount/Email")),
    //   img:"",
    // },
  
    // { // Manage password
    //   title: "Home",
    //   path: "/Manage/Changepassword",
    //   menu: true,
    //   requiresAuth: false,
    //   routes: customerRoutes,
    //   component: React.lazy(() => import(/* webpackChunkName: "InviteCustomer" */ "Pages/ManageAccount/ChangePassword")),
    //   img:"",
    // },
  
   ////////////// /////////////////// ////////////////////   //////////////////////////////

];
