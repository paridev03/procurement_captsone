import axios from 'axios';

// Single axios instance the whole app shares (Section 9, docs/ANALYSIS.md — API
// service layer). Nothing else in the app should call axios/fetch directly.
const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5080/api',
});

// Attach the JWT to every request once the user is logged in.
apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Centralized error normalization: the backend's ExceptionHandlingMiddleware always
// returns { status, title, traceId } — surface `title` as a plain message so every
// page can just do `catch (err) { setError(err.message) }`.
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    const status = error.response?.status;
    const message = error.response?.data?.title || error.message || 'Something went wrong.';

    if (status === 401) {
      localStorage.removeItem('token');
      localStorage.removeItem('user');
      if (window.location.pathname !== '/login') {
        window.location.assign('/login');
      }
    }

    return Promise.reject(new Error(message));
  }
);

export default apiClient;
