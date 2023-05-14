import { useEffect, useState } from "react";
import { UserInfoDTO, WhitelistService } from "../../../api";
import { promiseToast, updatePromiseToast } from "../../../lib";
import { TiDelete } from "react-icons/ti";

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
    <div className="flex h-full flex-col gap-4">
      <div className="flex flex-1 flex-row justify-between gap-2">
        <div className="w-1/2 rounded-xl bg-backgroundColor p-2">
          <h1 className="mb-2 h-16 text-center text-xl font-bold">
            Whitelisted Users
          </h1>
          <div className="bg-white flex flex-col gap-2 overflow-y-auto rounded-lg p-2">
            {whitelistedUsers.map((user) => (
              <div
                key={user.username}
                className="flex items-center justify-between text-ellipsis rounded bg-element p-2 text-lg shadow-xl"
              >
                <span className="w-5/6 truncate">{user.username}</span>
                <button
                  className="text-red-600 text-2xl font-bold hover:text-accent3"
                  onClick={() => moveUserToNonWhitelist(user)}
                >
                  <TiDelete />
                </button>
              </div>
            ))}
          </div>
        </div>
        <div className="w-1/2 rounded-xl bg-backgroundColor p-2">
          <h1 className="mb-2 h-16 text-center text-xl font-bold">
            Non-Whitelisted Users
          </h1>
          <div className="bg-white flex flex-col gap-2 overflow-y-auto rounded-lg p-2">
            {nonWhitelistedUsers.map((user) => (
              <div
                key={user.username}
                className="flex items-center justify-between text-ellipsis rounded bg-element p-2 text-lg shadow-xl"
              >
                <span className="w-5/6 truncate">{user.username}</span>
                <button
                  className="text-red-600 text-2xl font-bold hover:text-accent3"
                  onClick={() => moveUserToWhitelist(user)}
                >
                  <TiDelete />
                </button>
              </div>
            ))}
          </div>
        </div>
      </div>
      <button
        onClick={update}
        className="base-bg-gradient-r m-2 h-12 self-end rounded-lg p-3 text-xl font-bold duration-150 hover:scale-105"
      >
        Save
      </button>
    </div>
  );
};

export default WhiteListPage;
