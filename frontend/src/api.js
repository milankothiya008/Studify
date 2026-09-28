import axios from "axios";

// Address of the .NET backend. You can change it in a ".env" file (see .env.example).
// A "/" or "/api" at the end is removed, so both "https://x.app" and "https://x.app/api/" work.
function cleanApiUrl(url) {
  let clean = url.trim();
  while (clean.endsWith("/")) {
    clean = clean.slice(0, -1);
  }
  if (clean.endsWith("/api")) {
    clean = clean.slice(0, -4);
  }
  return clean;
}

export const API_URL = cleanApiUrl(import.meta.env.VITE_API_URL || "http://localhost:5000");

// One axios object for all API calls. Every URL starts with /api
const api = axios.create({
  baseURL: API_URL + "/api",
});

// Before every request: add the login token (if the user is logged in).
api.interceptors.request.use(function (config) {
  const token = localStorage.getItem("token");
  if (token) {
    config.headers.Authorization = "Bearer " + token;
  }
  return config;
});

// After every response: if the login has expired, log the user out.
// (A login also expires when the password or role changes.)
api.interceptors.response.use(
  function (response) {
    return response;
  },
  function (error) {
    const hadToken = localStorage.getItem("token");
    // The /auth/... pages (login, verify, reset, me) handle their own errors.
    const isAuthRequest = error.config && error.config.url.startsWith("/auth/");

    if (error.response && error.response.status === 401 && hadToken && !isAuthRequest) {
      localStorage.removeItem("token");
      window.location.href = "/login?expired=1";
    }
    return Promise.reject(error);
  }
);

// Turns any API error into a message we can show on the screen.
export function getErrorMessage(error) {
  if (error.response && error.response.data) {
    const data = error.response.data;

    // Our own errors look like { message: "..." }
    if (data.message) {
      return data.message;
    }

    // ASP.NET validation errors look like { errors: { Email: ["..."] } }
    if (data.errors) {
      const firstField = Object.keys(data.errors)[0];
      return data.errors[firstField][0];
    }
  }

  if (!error.response) {
    return "Cannot reach the server. Is the backend running on " + API_URL + "?";
  }

  return "Something went wrong. Please try again.";
}

export default api;
