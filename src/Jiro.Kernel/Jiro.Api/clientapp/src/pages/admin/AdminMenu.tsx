import { AiFillFire } from "react-icons/ai";
import { NavLink } from "react-router-dom";

const AdminMenu = () => {
  const routes = [
    {
      path: "users",
      text: "users",
      icon: <AiFillFire className="inline" />,
    },
    {
      path: "whitelist",
      text: "Whitelist",
      icon: <AiFillFire className="inline" />,
    },
  ];

  return (
    <>
      {routes.map((route, index) => {
        return (
          <NavLink
            to={route.path}
            className="rounded-lg bg-element p-2 font-bold text-accent3 duration-200 hover:scale-110"
            key={index}
          >
            {route.icon} {route.text}
          </NavLink>
        );
      })}
    </>
  );
};

export default AdminMenu;
