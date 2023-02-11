import { useState } from "react";
import { GiHamburgerMenu } from "react-icons/gi";
import { HiOutlineX } from "react-icons/hi";
import { NavLink } from "react-router-dom";

const Menu = () => {
  const [isOpen, setIsOpen] = useState(false);
  const menuItems = [
    {
      value: "Home",
    },
    {
      value: "Home",
    },
    {
      value: "Home",
    },
    {
      value: "Home",
    },
  ];
  return (
    <>
      <div
        className={`fixed z-50 flex h-full w-[256px] flex-col bg-element bg-opacity-90 transition duration-300 backdrop-blur-sm${
          isOpen ? "" : " translate-x-[-256px]"
        }`}
      >
        <div className="relative flex w-full flex-col gap-2 pt-12">
          {menuItems.map((item, index) => (
            <NavLink to="/" className="p-4" key={index}>
              {item.value}
            </NavLink>
          ))}
          <div
            className="absolute top-4 right-4 text-3xl transition duration-150 hover:scale-125 hover:cursor-pointer"
            onClick={() => setIsOpen(!isOpen)}
          >
            <HiOutlineX />
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
