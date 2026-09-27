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
      .catch(function () {
        localStorage.removeItem("token");
      })
      .finally(function () {
        setLoading(false);
      });
  }, []);

  async function login(email, password) {
    const response = await api.post("/auth/login", { email: email, password: password });
    localStorage.setItem("token", response.data.token);
    setUser(response.data.user);
    return response.data.user;
  }

  async function register(fullName, email, password, role) {
    const response = await api.post("/auth/register", {
      fullName: fullName,
      email: email,
      password: password,
      role: role,
    });
    localStorage.setItem("token", response.data.token);
    setUser(response.data.user);
    return response.data.user;
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
    logout: logout,
    setUser: setUser, // used by the profile page after an update
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  return useContext(AuthContext);
}
