import apiClient from './apiClient';

export async function login(email, password) {
  const { data } = await apiClient.post('/auth/login', { email, password });
  return data; // { token, expiresAt, user }
}
