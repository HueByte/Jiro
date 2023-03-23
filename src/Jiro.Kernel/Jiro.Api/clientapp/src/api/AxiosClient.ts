import axios, { AxiosRequestConfig, AxiosResponse } from "axios";
import { AuthService } from "./services/AuthService";

axios.interceptors.response.use(
  (Response) => responseHandler(Response),
  (Error) => errorHandler(Error)
);

let isRefreshing = false;
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
  config: AxiosRequestConfigWithRetry;
}): Promise<any> {
  const _axios = axios;
  const originalRequest = error.config;

  if (error.response.status === 401 && !originalRequest._retry) {
    if (isRefreshing) {
      return new Promise(function (resolve, reject) {
        failedQueue.push({ resolve, reject });
      })
        .then(() => {
          return _axios.request(originalRequest);
        })
        .catch((err) => {
          return Promise.reject(err);
        });
    }

    originalRequest._retry = true;
    isRefreshing = true;

    return new Promise((resolve, reject) => {
      AuthService.postApiAuthRefreshToken()
        .then((result) => {
          if (result.isSuccess) {
            processQueue(null);
            resolve(_axios(originalRequest));
          } else {
            processQueue(null);
            redirectToLogout();
            reject(error);
          }
        })
        .catch((err) => {
          processQueue(err);
          reject(err);
          redirectToLogout();
        })
        .then(() => {
          isRefreshing = false;
        });
    });
  }

  return Promise.reject(error);
}

function redirectToLogout() {
  isRefreshing = false;
  window.location.replace(
    `${window.location.protocol}//${window.location.host}/logout`
  );
}

interface AxiosRequestConfigWithRetry extends AxiosRequestConfig<any> {
  _retry?: boolean;
}

interface ApiResponse {
  data: any;
  errors: string[];
  isSuccess: boolean;
}
