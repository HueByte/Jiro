import axios, { AxiosRequestConfig } from "axios";

const axiosInstance = axios.create();

axios.interceptors.response.use(
  (Response) => Response,
  (Error) => errorHandler(Error)
);

function errorHandler(err: {
  response: { status: any; data: any };
  config: AxiosRequestConfig<any>;
}): Promise<any> {
  if (err.response.data) {
    return Promise.reject(err.response.data);
  }

  return Promise.reject(err);
}
