import { Outlet } from "react-router-dom";
import AdminMenu from "./AdminMenu";

const AdminPage = () => {
  return (
    <div className="flex h-full w-full justify-center py-4">
      <div className="flex w-11/12 max-w-[1024px] flex-row gap-2 rounded-xl bg-element p-2 shadow-lg shadow-element md:flex-col">
        <div className="flex min-h-[50px] min-w-[200px] flex-col gap-2 overflow-y-auto rounded-xl bg-elementLight p-2 md:flex-row md:justify-center">
          <AdminMenu />
        </div>
        <div className="w-full overflow-auto">
          <Outlet />
        </div>
      </div>
    </div>
  );
};

export default AdminPage;
