import { useEffect, useState } from "react";
import { AdminService, UserInfoDTO, WhitelistService } from "../../../api";
import { promiseToast, updatePromiseToast } from "../../../lib";

const WhiteListPage = () => {
  const [whitelistedUsers, setWhitelistedUsers] = useState<UserInfoDTO[]>([]);
  const [nonWhitelistedUsers, setNonWhitelistedUsers] = useState<UserInfoDTO[]>(
    []
  );
  const [modifiedUsers, setModifiedUsers] = useState<UserInfoDTO[]>([]);

  useEffect(() => {
    async function fetchUsers() {
      let id = promiseToast("Loading users...");
      try {
        let result = await WhitelistService.getApiWhitelistUsers();

        if (result.data && result.isSuccess) {
          updatePromiseToast(id, "Users loaded!", "success");

          setWhitelistedUsers(
            result.data.filter((user: UserInfoDTO) => user.isWhitelisted)
          );

          setNonWhitelistedUsers(
            result.data.filter((user: UserInfoDTO) => !user.isWhitelisted)
          );
        } else {
          updatePromiseToast(
            id,
            `Failed to load users!\n${result.errors?.join("\n")}`,
            "error"
          );
        }
      } catch (error: any) {
        let errBody = error.body;
        updatePromiseToast(
          id,
          `Failed to load users!\n${errBody.errors?.join("\n")}`,
          "error"
        );
      }
    }

    fetchUsers();
  }, []);

  useEffect(() => console.log(modifiedUsers), [modifiedUsers]);

  const moveUserToWhitelist = (user: UserInfoDTO) => {
    user.isWhitelisted = true;

    setWhitelistedUsers([...whitelistedUsers, user]);
    setNonWhitelistedUsers(
      nonWhitelistedUsers.filter((u) => u.username !== user.username)
    );

    if (!modifiedUsers.some((u) => u.username === user.username)) {
      setModifiedUsers([...modifiedUsers, user]);
    } else {
      setModifiedUsers(
        modifiedUsers.filter((u) => u.username !== user.username)
      );
    }
  };

  const moveUserToNonWhitelist = (user: UserInfoDTO) => {
    user.isWhitelisted = false;

    setNonWhitelistedUsers([...nonWhitelistedUsers, user]);
    setWhitelistedUsers(
      whitelistedUsers.filter((u) => u.username !== user.username)
    );

    if (!modifiedUsers.some((u) => u.username === user.username)) {
      setModifiedUsers([...modifiedUsers, user]);
    } else {
      setModifiedUsers(
        modifiedUsers.filter((u) => u.username !== user.username)
      );
    }
  };

  const update = async () => {
    let id = promiseToast("Updating whitelist...");
    console.log(modifiedUsers);
    try {
      let result = await WhitelistService.putApiWhitelistWhitelistedUsers({
        requestBody: modifiedUsers,
      });
      if (result.isSuccess) {
        updatePromiseToast(id, "Whitelist updated!", "success");
      } else {
        updatePromiseToast(
          id,
          `Failed to update whitelist!\n${result.errors?.join("\n")}`,
          "error"
        );
      }
    } catch (error: any) {
      let errBody = error.body;
      updatePromiseToast(
        id,
        `Failed to update whitelist!\n${errBody.errors?.join("\n")}`,
        "error"
      );
    }
  };

  return (
    <div className="flex h-full flex-col">
      <div className="flex flex-1 flex-row justify-between p-4">
        <div className="w-1/2">
          <h2 className="mb-2 text-lg font-bold">Whitelisted Users</h2>
          <ul className="bg-white rounded-lg p-2 shadow-md">
            {whitelistedUsers.map((user) => (
              <li
                key={user.username}
                className="flex items-center justify-between border-b py-2"
              >
                <span>{user.username}</span>
                <button
                  className="text-red-600 font-bold"
                  onClick={() => moveUserToNonWhitelist(user)}
                >
                  Remove from whitelist
                </button>
              </li>
            ))}
          </ul>
        </div>
        <div className="w-1/2">
          <h2 className="mb-2 text-lg font-bold">Non-Whitelisted Users</h2>
          <ul className="bg-white rounded-lg p-2 shadow-md">
            {nonWhitelistedUsers.map((user) => (
              <li
                key={user.username}
                className="flex items-center justify-between border-b py-2"
              >
                <span>{user.username}</span>
                <button
                  className="text-green-600 font-bold"
                  onClick={() => moveUserToWhitelist(user)}
                >
                  Add to whitelist
                </button>
              </li>
            ))}
          </ul>
        </div>
      </div>
      <button
        onClick={update}
        className="base-bg-gradient-r h-12 self-end rounded-lg p-3 text-xl font-bold duration-150 hover:scale-105"
      >
        Save
      </button>
    </div>
  );
};

export default WhiteListPage;
