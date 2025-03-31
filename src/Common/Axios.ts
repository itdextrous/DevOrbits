import { oidcUserManager } from "Services/Security/OidcUserManager";
import axios from "axios";
// import parseApiError from "Common/Utility/ParseApiError";

const axiosInstance = axios.create({
  baseURL: "/api",
});

// eslint-disable-next-line @typescript-eslint/no-explicit-any
// function showApiError(error: any) {
//   const { message, title } = parseApiError(error);

//   // const dialogConfig = getDialogErrorConfig(
//   //   `<b>${title}</b><br/>
//   //   ${message}<br/>
//   //   ${error.config.method.toUpperCase()} request to ${error.config.url}`,
//   // );

//   // Dialog.alert(dialogConfig);
// }

// Use request interceptor to add Bearer token to API requests
axiosInstance.interceptors.request.use(
  async (config) => {
    const user = await oidcUserManager.getUser();

    if (user?.access_token) {
      // eslint-disable-next-line no-param-reassign
      config.headers = {
        Authorization: `Bearer ${user.access_token}`,
      };
    }

    return config;
  },
  (error: any) => {
    Promise.reject(error);
  },
);

axiosInstance.interceptors.response.use((response) => response, async (error) => {
  // Use response interceptor to trigger signin if we get 40x unauthorized response
  if (error?.response?.status === 401 || error?.response?.status === 403) {
    oidcUserManager.signinRedirect();
  }
  // else {
  //   showApiError(error);
  // }

  Promise.reject(error);
});

export default axiosInstance;
