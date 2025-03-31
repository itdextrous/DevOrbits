import Qs from "qs";
import axiosInstance from "Common/Axios";
import { AxiosRequestConfig, AxiosResponse } from "axios";

export async function apiGet<responseT>(resource: string, queryString = "", config: AxiosRequestConfig | undefined = undefined) {
  const response = await axiosInstance.get<null, AxiosResponse<responseT>>(`${resource}?${queryString}`, config);
  return response?.data;
}

export async function apiPost<T, responseT>(resource: string, request: T, config: AxiosRequestConfig | undefined = undefined) {
  const response = await axiosInstance.post<T, AxiosResponse<responseT>>(resource, request, config);
  return response?.data;
}

export async function apiPut<T, responseT>(resource: string, entity: T, config: AxiosRequestConfig | undefined = undefined) {
  const response = await axiosInstance.put<T, AxiosResponse<responseT>>(resource, entity, config);
  return response?.data;
}

export async function apiDelete<responseT>(resource: string, config: AxiosRequestConfig | undefined = undefined) {
  const response = await axiosInstance.delete<String, AxiosResponse<responseT>>(resource, config);
  return response?.data;
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export function stringify(criteria: any) {
  return Qs.stringify(criteria, { allowDots: true, skipNulls: true });
}
