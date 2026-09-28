import { createContext, useContext, useEffect, useState } from "react";
import api from "./api";

// Keeps the logged in user in one place, so every page can read it with useAuth().
const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);

  // When the app starts: if we have a saved token, load the user.
  useEffect(function () {
    const token = localStorage.getItem("token");
    if (!token) {
      setLoading(false);
      return;
    }

    api
      .get("/auth/me")
      .then(function (response) {
        setUser(response.data);
      })
      .catch(function (error) {
        // Only a rejected login (401) is removed. For other problems (server down,
        // too many requests ...) the token is kept, so the user stays logged in.
        if (error.response && error.response.status === 401) {
          localStorage.removeItem("token");
        }
      })
      .finally(function () {
        setLoading(false);
      });
  }, []);

  // Saves the token + user that the API sends back after logging in.
  function saveLogin(data) {
    localStorage.setItem("token", data.token);
    setUser(data.user);
    return data.user;
  }

  async function login(email, password) {
    const response = await api.post("/auth/login", { email: email, password: password });
    return saveLogin(response.data);
  }

  // Creates the account. The user is NOT logged in yet: they must type the
  // code we emailed first (see verifyEmail). Returns { requiresVerification, email, message }.
  async function register(fullName, email, password, role) {
    const response = await api.post("/auth/register", {
      fullName: fullName,
      email: email,
      password: password,
      role: role,
    });
    return response.data;
  }

  async function verifyEmail(email, code) {
    const response = await api.post("/auth/verify-email", { email: email, code: code });
    return saveLogin(response.data);
  }

  async function resetPassword(email, code, newPassword) {
    const response = await api.post("/auth/reset-password", {
      email: email,
      code: code,
      newPassword: newPassword,
    });
    return saveLogin(response.data);
  }

  // Used after changing the password: the API sends a fresh token for this device.
  function saveToken(token) {
    localStorage.setItem("token", token);
  }

  function logout() {
    localStorage.removeItem("token");
    // Reload the app on the home page. This clears everything kept in memory
    // (and avoids being sent to the login page when logging out on a private page).
    window.location.href = "/";
  }

  const value = {
    user: user,
    loading: loading,
    login: login,
    register: register,
    verifyEmail: verifyEmail,
    resetPassword: resetPassword,
    saveToken: saveToken,
    logout: logout,
    setUser: setUser, // used by the profile page after an update
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  return useContext(AuthContext);
}
