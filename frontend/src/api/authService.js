import apiClient from './apiClient';

export async function login(email, password) {
  const { data } = await apiClient.post('/auth/login', { email, password });
  return data; // { token, expiresAt, user }
}

export async function register(fullName, email, password, role, department) {
  const { data } = await apiClient.post('/auth/register', { fullName, email, password, role, department });
  return data; // { token, expiresAt, user } — same shape as login, so registering signs you in
}
