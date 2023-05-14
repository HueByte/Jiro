import { AiFillFire } from "react-icons/ai";
import { RiFileList2Fill } from "react-icons/ri";
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
      icon: <RiFileList2Fill className="inline" />,
    },
  ];

  return (
    <>
      {routes.map((route, index) => {
        return (
          <NavLink
            to={route.path}
            className="rounded-lg bg-element p-2 font-bold text-accent3 duration-200 hover:scale-105"
            key={index}
          >
            {route.icon} <span className="md:hidden">{route.text}</span>
          </NavLink>
        );
      })}
    </>
  );
};

export default AdminMenu;
