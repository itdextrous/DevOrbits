import React from "react";
import { Dropdown } from "react-bootstrap";
import { LinkContainer } from "react-router-bootstrap";
import { Link } from "react-router-dom";
import { MenuRoute } from "Routes";

interface Props {
  menuItem: MenuRoute;
  parentPath?: string;
  onClick?: React.MouseEventHandler<HTMLAnchorElement> | undefined
}

const defaultProps = {
  parentPath: "",
  onClick: undefined,
};

export default function NavMenuItem({ menuItem, parentPath, onClick }: Props) {
  if (!menuItem?.menu) { return null; }
  
  function getPath(parent: string | undefined, currentItem: MenuRoute): string {
    return (parent || "") + (currentItem.menuPath ? currentItem.menuPath : currentItem.path);
  }

  const currentPath = getPath(parentPath, menuItem);
  const hasChildMenuItems = (menuItem.routes && menuItem.routes.length > 0);

  return (
    <li className="nav-item lft_nav">
      {!hasChildMenuItems ? (
        <>
          <Link to={currentPath} className="nav-link">  {menuItem.img ? <img alt="menuImage" src={menuItem.img} /> : ""} {menuItem.title}</Link>
        </>)
        : (
          <Dropdown>
            <Dropdown.Toggle
              as="a"
              className="nav-link"
              style={{ cursor: "pointer" }}
            >
              {menuItem.title}
            </Dropdown.Toggle>

            <Dropdown.Menu
              as="div"
              title={menuItem.title}
            >
              {menuItem.routes && menuItem.routes
                .filter((route) => route.menu)
                .map((item) => (
                  <LinkContainer
                    key={getPath(currentPath, item)}
                    to={getPath(currentPath, item)}
                    onClick={onClick}
                    exact
                  >
                    <Dropdown.Item>{item.title}</Dropdown.Item>
                  </LinkContainer>
                ))}
            </Dropdown.Menu>
          </Dropdown>
        )}

    </li>
  );
}

NavMenuItem.defaultProps = defaultProps;
