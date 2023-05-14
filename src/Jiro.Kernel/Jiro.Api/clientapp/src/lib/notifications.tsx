import { Id, toast } from "react-toastify";

export const infoToast = (
  message: string,
  autoClose: number | false = 3000
) => {
  toast(message, {
    position: toast.POSITION.TOP_RIGHT,
    autoClose: autoClose,
    hideProgressBar: false,
    closeOnClick: true,
    pauseOnHover: true,
    draggable: true,
    progress: undefined,
    style: {
      background: "#000c14",
    },
  });
};

export const promiseToast = (
  message: string,
  autoClose: number | false = 3000
): Id => {
  let id = toast.loading(message, {
    position: toast.POSITION.TOP_RIGHT,
    autoClose: autoClose,
    hideProgressBar: false,
    closeOnClick: true,
    pauseOnHover: true,
    draggable: true,
    progress: undefined,
    style: {
      background: "#000c14",
    },
  });

  return id;
};

export const updatePromiseToast = (
  id: Id,
  message: string,
  type: "success" | "error" | "warning",
  autoClose: number | false = 3000
) => {
  toast.update(id, {
    render: message,
    isLoading: false,
    type: type,
    autoClose: autoClose,
  });
};

export const successToast = (
  message: string,
  autoClose: number | false = 3000
) => {
  toast.success(message, {
    position: toast.POSITION.TOP_RIGHT,
    autoClose: autoClose,
    hideProgressBar: false,
    closeOnClick: true,
    pauseOnHover: true,
    draggable: true,
    progress: undefined,
    style: {
      background: "#000c14",
    },
  });
};

export const errorToast = (
  message: string,
  autoClose: number | false = 10000
) => {
  toast.error(message, {
    position: toast.POSITION.TOP_RIGHT,
    autoClose: autoClose,
    hideProgressBar: false,
    closeOnClick: true,
    pauseOnHover: true,
    draggable: true,
    progress: undefined,
    style: {
      background: "#000c14",
    },
  });
};

export const warningToast = (
  message: string,
  autoClose: number | false = 10000
) => {
  toast.warn(message, {
    position: toast.POSITION.TOP_RIGHT,
    autoClose: autoClose,
    hideProgressBar: false,
    closeOnClick: true,
    pauseOnHover: true,
    draggable: true,
    progress: undefined,
    style: {
      background: "#000c14",
    },
  });
};
