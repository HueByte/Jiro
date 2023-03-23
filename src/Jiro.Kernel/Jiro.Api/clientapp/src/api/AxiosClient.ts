import axios, { AxiosRequestConfig, AxiosResponse } from "axios";
import { AuthService } from "./services/AuthService";

const axiosApiInstance = axios.create();

axios.interceptors.response.use(
  (Response) => responseHandler(Response),
  (Error) => errorHandler(Error)
);

let isRefreshing = false;
let tokenApproved = true;
let failedQueue: any[] = [];

const processQueue = (error: any) => {
  failedQueue.forEach((prom) => {
    if (error) prom.reject(error);
    else prom.resolve();
  });

  failedQueue = [];
};

function responseHandler(response: AxiosResponse) {
  let result: ApiResponse = response.data;

  if (response.status == 200 && !result.isSuccess) {
    // errorModal(result?.errors.join("\n"), 10000);
    return response;
  }

  return response;
}

function errorHandler(error: {
  response: { status: any; data: any };
  config: AxiosRequestConfig<any>;
}): Promise<any> {
  const originalRequest = error.config;

  if (error.response.status !== 401) {
    return Promise.reject(error);
  }

  if (isRefreshing) {
    return new Promise((resolve, reject) => {
      failedQueue.push({ resolve, reject });
    })
      .then((result) => {
        return axios(originalRequest);
      })
      .catch((err) => {
        return Promise.reject(err);
      });
  }

  isRefreshing = true;

  return new Promise((resolve, reject) => {
    AuthService.postApiAuthRefreshToken()
      .then((result) => {
        if (result.isSuccess) {
          processQueue(null);
          resolve(axios(originalRequest));
        } else {
          processQueue(null);
          redirectToLogout();
          reject(error);
        }
      })
      .catch((err) => {
        processQueue(err);
        redirectToLogout();
        reject(err);
      })
      .then(() => {
        isRefreshing = false;
      });
  });
}

function redirectToLogout() {
  isRefreshing = false;
  window.location.replace(
    `${window.location.protocol}//${window.location.host}/logout`
  );
}

interface ApiResponse {
  data: any;
  errors: string[];
  isSuccess: boolean;
}
