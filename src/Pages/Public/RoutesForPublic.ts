import React from "react";
import { MenuRoute } from "Routes";

const publicRoutes: MenuRoute[] = [
  {
    title: "Apply as a Brokerage",
    path: "public/application/broker/BrokerComponent/:planId",
    menu: true,
    component: React.lazy(() => import(/* webpackChunkName: "public" */ "Pages/Public/Application/Broker/BrokerComponent")),
    img:"",
  },
  {
    title: "Apply as a Dealer",
    path: "public/application/dealer/DealerComponent/:planId",
    menu: true,
    component: React.lazy(() => import(/* webpackChunkName: "public" */ "Pages/Public/Application/Dealer/DealerComponent")),
    img:"",
  },
  // {
  //   title: "Mockups",
  //   path: "public/mockup/",
  //   menu: true,
  //   routes: mockupRoutes,
  //   img:"",
  //   component: React.lazy(() => import(/* webpackChunkName: "public" */ "Pages/Public/Application/Dealer/Dealer")),
  // },
  {
    title: "Signup",
    path: "public/signup/:planId",
    menu: true,
    img:"",
    component: React.lazy(() => import(/* webpackChunkName: "public" */ "Pages/Public/Signup/Signup")),
  }
];

export default publicRoutes;
