import React from "react";
import { MenuRoute } from "Routes";

const adminRoutes: MenuRoute[] = [
  {
    title: "Dev test",
    path: "admin/test",
    menu: false,
    component: React.lazy(() => import(/* webpackChunkName: "admin" */ "Pages/Admin/Info")),
    img:"",
  },
  {
    title: "Brokerages",
    path: "admin/brokerage/",
    menu: true,
    component: React.lazy(() => import(/* webpackChunkName: "admin" */ "Pages/Admin/Brokerage/Search")),
    img:"",
    routes: [
      {
        title: "Search",
        path: "search",
        menu: true,
        component: React.lazy(() => import(/* webpackChunkName: "admin" */ "Pages/Admin/Brokerage/Search")),
        img:"",
      },
      {
        title: "Add",
        path: "edit/:brokerageId?",
        menuPath: "edit",
        menu: true,
        img:"",
        component: React.lazy(() => import(/* webpackChunkName: "admin" */ "Pages/Admin/Brokerage/Edit")),
      },
      {
        title: "Applications",
        path: "application/search",
        component: React.lazy(() => import(/* webpackChunkName: "admin" */ "Pages/Admin/Brokerage/Application/Search")),
        menu: true,
        img:"",
      },
      {
        title: "Application Edit",
        path: "application/edit/:brokerageApplicationId?",
        menuPath: "application/edit/",
        component: React.lazy(() => import(/* webpackChunkName: "admin" */ "Pages/Admin/Brokerage/Application/Edit")),
        menu: false,
        img:"",
      },
    ],
  },
  {
    title: "Brokers",
    path: "admin/broker/",
    menu: false,
    img:"",
    component: React.lazy(() => import(/* webpackChunkName: "admin" */ "Pages/Admin/Brokerage/Search")),
    routes: [
      {
        title: "Add",
        path: "add/:brokerageId",
        menuPath: "add",
        img:"",
        menu: false,
        component: React.lazy(() => import(/* webpackChunkName: "admin" */ "Pages/Admin/Broker/Edit")),
      },
      {
        title: "Edit",
        path: "edit/:brokerId?",
        menuPath: "edit",
        menu: false,
        img:"",
        component: React.lazy(() => import(/* webpackChunkName: "admin" */ "Pages/Admin/Broker/Edit")),
      },
    ],
  },
  {
    title: "Dealers",
    path: "admin/dealer/",
    component: React.lazy(() => import(/* webpackChunkName: "admin" */ "Pages/Admin/Dealer/Search")),
    menu: true,
    img:"",
    routes: [
      {
        title: "Search",
        path: "search",
        img:"",
        component: React.lazy(() => import(/* webpackChunkName: "admin" */ "Pages/Admin/Dealer/Search")),
        menu: true,
      },
      {
        title: "Add",
        path: "edit/:dealerId?",
        menuPath: "edit/",
        component: React.lazy(() => import(/* webpackChunkName: "admin" */ "Pages/Admin/Dealer/Edit")),
        menu: true,
        img:"",
      },
      {
        title: "Applications",
        path: "application/search",
        component: React.lazy(() => import(/* webpackChunkName: "admin" */ "Pages/Admin/Dealer/Application/Search")),
        menu: true,
        img:"",
      },
      {
        title: "Application Edit",
        path: "application/edit/:dealerApplicationId?",
        menuPath: "application/edit/",
        component: React.lazy(() => import(/* webpackChunkName: "admin" */ "Pages/Admin/Dealer/Application/Edit")),
        menu: false,
        img:"",
      },
    ],
  },
  {
    title: "Stocks",
    path: "stocks/UploadStocksCSV/",
    component: React.lazy(() => import(/* webpackChunkName: "admin" */ "Pages/Stocks/uploadStocks")),
    menu: true,
    img:"",
    routes: [],
  },
];
export default adminRoutes;
