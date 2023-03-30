import { Outlet } from "react-router-dom";

const AdminPage = () => {
  return (
    <div className="flex h-full w-full justify-center px-8 py-8 lg:px-0">
      <div className="flex h-full w-5/6 max-w-[1024px] flex-col rounded-xl bg-element p-2 pt-6 shadow-lg shadow-element lg:w-11/12">
        <div>Menu</div>
        <Outlet />
      </div>
    </div>
  );
};

export default AdminPage;
