import { Outlet } from "react-router-dom";
import AdminMenu from "./AdminMenu";

const AdminPage = () => {
  return (
    <div className="flex h-full w-full justify-center px-8 py-8 lg:px-0">
      <div className="flex h-full w-5/6 max-w-[1024px] flex-row overflow-hidden rounded-xl bg-element shadow-lg shadow-element lg:w-11/12 md:flex-col">
        <div className="flex w-1/6 flex-col gap-2 bg-backgroundColorLight p-4 md:flex-row">
          <AdminMenu />
        </div>
        <div className="w-5/6 p-2">
          <Outlet />
        </div>
      </div>
    </div>
  );
};

export default AdminPage;
