import { useState, useContext } from "react";
import { GiHamburgerMenu } from "react-icons/gi";
import { HiOutlineX, HiServer } from "react-icons/hi";
import { NavLink } from "react-router-dom";
import { AiFillFire } from "react-icons/ai";
import { MdLogout } from "react-icons/md";
import { Roles } from "../api/Roles";
import { AuthContext } from "../contexts/AuthContext";
import { BiUser } from "react-icons/bi";
import { RiAdminLine } from "react-icons/ri";

const Menu = () => {
  const auth = useContext(AuthContext);
  const [isOpen, setIsOpen] = useState(false);

  const menuItems = [
    {
      icon: <AiFillFire className="inline" />,
      path: "/",
      value: "Home",
      roles: [],
    },
    {
      icon: <RiAdminLine className="inline" />,
      path: "/admin",
      value: "Admin Panel",
      roles: [Roles.ADMIN],
    },
    {
      icon: <HiServer className="inline" />,
      path: "/server",
      value: "Server Panel",
      roles: [Roles.SERVER],
    },
    {
      icon: <MdLogout className="inline" />,
      path: "/logout",
      value: "Logout",
      roles: [],
    },
  ];
  return (
    <>
      <div
        className={`fixed z-50 flex h-full w-[400px] flex-col bg-backgroundColor bg-opacity-70 transition duration-300 md:w-full backdrop-blur-sm${
          isOpen ? "" : " translate-x-[-400px] md:-translate-x-full"
        }`}
      >
        <div className="relative flex w-full flex-1 flex-col gap-2 pt-14 md:items-center">
          {menuItems.map((item, index) =>
            auth?.isInRole(item.roles) ? (
              <NavLink
                to={item.path}
                onClick={() => setIsOpen(!isOpen)}
                className={(navData) =>
                  `${
                    navData.isActive ? "menu-active " : ""
                  }p-4 text-xl font-bold transition duration-300 hover:bg-accent6 md:w-full md:text-center md:text-3xl`
                }
                key={index}
              >
                {item.icon} {item.value}
              </NavLink>
            ) : (
              <></>
            )
          )}
          <div
            className="absolute top-4 right-4 text-3xl transition duration-150 hover:scale-125 hover:cursor-pointer"
            onClick={() => setIsOpen(!isOpen)}
          >
            <HiOutlineX />
          </div>
        </div>
        <div className="grid place-items-center bg-element p-4 text-3xl">
          <div className="text-ellipsis font-bold">
            <BiUser className="inline" /> {auth?.authState?.username}
          </div>
        </div>
      </div>
      <div
        className={`absolute top-4 left-4 text-3xl transition duration-1000 hover:scale-125 hover:cursor-pointer${
          isOpen ? " opacity-0" : ""
        }`}
        onClick={() => setIsOpen(!isOpen)}
      >
        <GiHamburgerMenu />
      </div>
    </>
  );
};

export default Menu;
