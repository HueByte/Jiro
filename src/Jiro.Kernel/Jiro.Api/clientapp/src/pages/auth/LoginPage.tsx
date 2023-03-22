import banner from "../../assets/JiroBanner.png";
import { MdLockOutline, MdPerson } from "react-icons/md";
import { AuthService } from "../../api";
import { useContext, useState } from "react";
import { AuthContext } from "../../contexts/AuthContext";
import { Navigate } from "react-router-dom";

const LoginPage = (): JSX.Element => {
  const [username, setUsername] = useState<string>("");
  const [password, setPassword] = useState<string>("");
  const auth = useContext(AuthContext);

  const submitLogin = async () => {
    let result = await AuthService.postApiAuthLogin({
      requestBody: {
        username: username,
        password: password,
      },
    });

    if (result.isSuccess) {
      if (result.data) auth?.setAuthState(result.data);
    }
  };

  if (auth?.isAuthenticated()) return <Navigate to="/" />;

  return (
    <div className="grid h-screen w-full place-items-center">
      <div className="md:base-border-gradient-r flex h-full max-h-[624px] w-full max-w-[400px] flex-col rounded-xl rounded-t-xl bg-element shadow-lg shadow-element md:max-h-full md:max-w-full md:rounded-none">
        <div className="h-[160px] w-full overflow-hidden rounded-t-xl">
          <div className="grid h-full w-full place-items-center bg-textColorLight">
            <img src={banner} className="h-[160px]" alt="Jiro Banner" />
          </div>
        </div>
        <div className="flex w-full flex-1 flex-col gap-6 rounded-b-xl bg-element bg-cover p-4 md:pt-16">
          <h1 className="text-center text-3xl font-bold">Sign in</h1>
          <div className="flex w-full flex-col gap-2">
            <label>Type your username</label>
            <div className="relative w-full">
              <input
                placeholder="username"
                type="text"
                className="base-input w-full pl-8"
                onInput={(e) => setUsername(e.currentTarget.value)}
              />
              <MdPerson className="absolute top-1/2 left-2 -translate-y-1/2" />
            </div>
            <label>Type your password</label>
            <div className="relative w-full">
              <input
                placeholder="password"
                type="password"
                className="base-input w-full pl-8"
                onInput={(e) => setPassword(e.currentTarget.value)}
              />
              <MdLockOutline className="absolute top-1/2 left-2 -translate-y-1/2" />
            </div>
          </div>
          <button
            type="button"
            className="base-bg-gradient-r rounded-lg p-3 text-xl font-bold duration-150 hover:scale-105"
            onClick={submitLogin}
          >
            Continue
          </button>
          <div className="text-center">
            Don't have account?{" "}
            <span className="font-bold text-accent3">Sing up</span>
          </div>
        </div>
      </div>
    </div>
  );
};

export default LoginPage;
