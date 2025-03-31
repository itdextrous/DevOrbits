import React from "react";
import { MenuRoute } from "Routes";

const customerRoutes: MenuRoute[] = [
  {
    title: "Vehicles",
    path: "customer/vehicle/search",
    menu: true,
    img:"",
    component: React.lazy(() => import(/* webpackChunkName: "broker" */ "Pages/Customer/Vehicle/Search")),
  },
  {
    title: "Messages",
    path: "customer/messages",
    menu: true,
    img:"",
    component: React.lazy(() => import(/* webpackChunkName: "customer" */ "Pages/Customer/Messaging/InboxConvo")),
  },
  {
    title: "Vehicle Detail",
    path: "customer/vehicle/detail/:stockId",
    menu: false,
    img:"",
    component: React.lazy(() => import(/* webpackChunkName: "broker" */ "Pages/Customer/Vehicle/Detail")),
  },
  {
    title: "Saved Vehicles",
    path: "customer/vehicle/saved",
    menu: true,
    img:"",
    component: React.lazy(() => import(/* webpackChunkName: "broker" */ "Pages/Customer/Vehicle/Saved")),
  },
  {
    title: "Saved Searches",
    path: "customer/search/saved",
    menu: true,
    img:"",
    component: React.lazy(() => import(/* webpackChunkName: "broker" */ "Pages/Customer/SavedSearch/List")),
  },
  {
    title: "Secure Vehicle",
    path: "customer/vehicle/secure/:stockId?",
    menu: false,
    img:"",
    component: React.lazy(() => import(/* webpackChunkName: "customer" */ "Pages/Mockup/Customer/SecureVehicle")),
  },
  {
    title: "Messages",
    path: "customer/messages/conversation/:conversationId",
    menu: false,
    img:"",
    component: React.lazy(() => import(/* webpackChunkName: "customer" */ "Pages/Customer/Messaging/CustomerConversation")),
  },
];

export default customerRoutes;