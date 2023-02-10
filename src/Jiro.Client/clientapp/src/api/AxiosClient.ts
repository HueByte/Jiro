import axios from "axios";

const axiosInstance = axios.create();

interface ApiResponse {
    data: any;
    errors: string[];
    isSuccess: boolean;
}

export { };