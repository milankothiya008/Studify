import { createContext, useContext, useEffect, useState } from "react";
import api from "./api";
import { useAuth } from "./AuthContext";

// Remembers which courses the logged in user saved (the heart buttons),
// so every page can show filled or empty hearts without asking the server again.
const WishlistContext = createContext(null);

export function WishlistProvider({ children }) {
  const { user } = useAuth();
  const [savedIds, setSavedIds] = useState([]);

  // Load the saved course ids when the user logs in.
  useEffect(
    function () {
      if (!user) {
        setSavedIds([]);
        return;
      }
      api
        .get("/wishlist/ids")
        .then(function (response) {
          setSavedIds(response.data);
        })
        .catch(function () {
          setSavedIds([]);
        });
    },
    [user]
  );

  function isSaved(courseId) {
    return savedIds.includes(courseId);
  }

  // Adds or removes a course. Returns true when the course is now saved.
  async function toggle(courseId) {
    if (isSaved(courseId)) {
      await api.delete("/wishlist/" + courseId);
      setSavedIds(savedIds.filter((id) => id !== courseId));
      return false;
    }
    await api.post("/wishlist/" + courseId);
    setSavedIds([...savedIds, courseId]);
    return true;
  }

  return (
    <WishlistContext.Provider value={{ savedIds: savedIds, isSaved: isSaved, toggle: toggle }}>
      {children}
    </WishlistContext.Provider>
  );
}

export function useWishlist() {
  return useContext(WishlistContext);
}
