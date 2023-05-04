import { Outlet } from "react-router-dom";
import AdminMenu from "./AdminMenu";
import { GiHamburgerMenu } from "react-icons/gi";
import { useState } from "react";

const AdminPage = () => {
  const [showMenu, setShowMenu] = useState(false);

  return (
    // <div className="flex h-full w-full justify-center px-4 py-4 lg:px-0">
    //   <div className="flex h-full w-11/12 max-w-[1024px] flex-row overflow-hidden rounded-xl bg-element shadow-lg shadow-element md:w-full md:flex-col">
    //     <div className="flex w-96 flex-col gap-2 bg-backgroundColorLight p-2 md:w-full">
    //       <AdminMenu />
    //     </div>
    //     <div className="w-full p-2">
    //       <Outlet />
    //     </div>
    //   </div>
    // </div>
    <div className="flex h-full w-full justify-center py-4">
      <div className="flex w-11/12 max-w-[1024px] flex-row gap-2 rounded-xl bg-element p-2 shadow-lg shadow-element md:flex-col">
        <div className="relative flex min-h-[50px] min-w-[200px] flex-col overflow-y-auto rounded-xl bg-elementLight p-2">
          <div
            className={`flex flex-col gap-2 md:pt-8 md:hidden${
              showMenu ? "md:block" : ""
            }`}
          >
            <AdminMenu />
          </div>
          <div className="absolute top-2 right-5 hidden text-3xl md:block">
            <GiHamburgerMenu onClick={() => setShowMenu(!showMenu)} />
          </div>
        </div>
        <div className="w-full overflow-auto">
          <Outlet />
        </div>
      </div>
    </div>
  );
};

export default AdminPage;
