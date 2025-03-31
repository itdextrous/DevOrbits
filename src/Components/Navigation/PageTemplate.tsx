import { ReactNode, useEffect, useState } from "react";
import Icon from "Components/Icon";
import BrandImage from "Images/New_logo.svg";
import { appRoutes, RoleRoute } from "Routes";
import { useAuthentication } from "Common/Authorization/ProvideAuthorization";
import { Link } from "react-router-dom";
import NavMenuItem from "./NavMenuItem";
import UserMenu from "./UserMenu";

interface Props {
  children: ReactNode;
}

export default function PageTemplate({ children }: Props) {
  const [showMenu, setShowMenu] = useState(false);
  const [routeConfig, setRouteConfig] = useState<RoleRoute>();
  const auth = useAuthentication();

  useEffect(() => {
    let foundRouteConfig;
    if (auth.isAuthenticated) {
      // Find top level menu with the required role
      foundRouteConfig = appRoutes.find((route) => route.requiredRole === auth.role);
    } else {
      // Find public menu root item
      foundRouteConfig = appRoutes.find((route) => !route.requiresAuth);
    }
    setRouteConfig(foundRouteConfig);
  }, [auth]);

  function handleNavMenuItemClick() {
    setShowMenu(false);
  }

  return (
    <div className="layout layout-nav-top">
        <header className="header">
            <div className="container">
      <div className="navbar navbar-expand-lg  sticky-top">
        <Link className="navbar-brand" to="/">
          <img alt="Logo" src={BrandImage} />
        </Link>
        <div className="d-flex align-items-center">
          <button
            className="navbar-toggler"
            type="button"
            data-toggle="collapse"
            data-target="#navbar-collapse"
            aria-controls="navbar-collapse"
            aria-expanded="false"
            aria-label="Toggle navigation"
            onClick={() => setShowMenu((currentState) => !currentState)}
          >
            <span className="navbar-toggler-icon">
              <Icon icon="bars" size="lg" />
            </span>
          </button>
        </div>
        <div className={`collapse navbar-collapse justify-content-between ${showMenu ? "show" : ""}`} id="navbar-collapse">
          <ul className="navbar-nav">

            {routeConfig?.routes && routeConfig.routes
              .filter((item) => item.menu)
              .map((route) => (
                <NavMenuItem
                  key={route.path}
                  menuItem={route}
                  parentPath={routeConfig.path}
                  onClick={handleNavMenuItemClick}
                />
              ))}
          </ul>
          <div className="d-lg-flex align-items-center">
            <UserMenu />
          </div>
        </div>
      </div>
      </div>
        </header>
      <div className="main-container">
        <div className="container justify-content-center" style={{ paddingTop: "15px" }}>
          {children}
        </div>
      </div>
    </div>
  );
}
