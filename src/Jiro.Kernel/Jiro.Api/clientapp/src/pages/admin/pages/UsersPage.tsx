import { useEffect, useState } from "react";
import { AdminService, UserInfoDTO } from "../../../api";
import { promiseToast, updatePromiseToast } from "../../../lib";

const UsersPage = () => {
  const [users, setUsers] = useState<UserInfoDTO[]>([]);
  useEffect(() => {
    (async () => {
      let id = promiseToast("Loading users...");
      try {
        let result = await AdminService.getApiAdminUsers();
        if (result.isSuccess && result.data) {
          setUsers(result.data);
          updatePromiseToast(id, "Users loaded!", "success");
        } else {
          updatePromiseToast(
            id,
            `Failed to load users!\n${result.errors?.join("\n")}`,
            "error"
          );
        }
      } catch (err: any) {
        let errBody = err.body;
        updatePromiseToast(
          id,
          `Failed to load users!\n${errBody?.errors.join("\n")}`,
          "error"
        );
      }
    })();
  }, []);
  return (
    <div className="relative w-full overflow-auto rounded-xl shadow-lg">
      <table className="w-full border-collapse text-left text-sm text-textColor">
        <thead className="bg-backgroundColorLight text-base uppercase text-accent3">
          <tr>
            <th scope="col" className="px-6 py-3">
              Username
            </th>
            <th scope="col" className="px-6 py-3">
              Email
            </th>
            <th scope="col" className="px-6 py-3">
              Roles
            </th>
            <th scope="col" className="px-6 py-3">
              Created
            </th>
            <th scope="col" className="px-6 py-3">
              Whitelisted
            </th>
          </tr>
        </thead>
        <tbody>
          {users &&
            users.map((user, index) => (
              <tr
                key={index}
                className="cursor-pointer whitespace-nowrap px-6 py-4 font-medium transition duration-300 even:bg-altBackgroundColorLight hover:bg-neutralDarker"
              >
                <td className="px-6 py-4">{user.username}</td>
                <td className="px-6 py-4">{user.email}</td>
                <td className="px-6 py-4">{user.roles?.join(", ")}</td>
                <td className="py-4 px-6">
                  {new Date(user.accountCreatedDate ?? "").toLocaleDateString()}
                </td>
                <td className="py-4 px-6">
                  {user.isWhitelisted ? "True" : "False"}
                </td>
              </tr>
            ))}
        </tbody>
      </table>
    </div>
  );
};

export default UsersPage;
