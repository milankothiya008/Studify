import { useState } from "react";
import { Link, NavLink, useNavigate } from "react-router-dom";
import { useAuth } from "../AuthContext";
import Avatar from "./Avatar";

function Navbar() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [searchText, setSearchText] = useState("");
  const [menuOpen, setMenuOpen] = useState(false);

  function handleSearch(event) {
    event.preventDefault();
    navigate("/courses?search=" + encodeURIComponent(searchText.trim()));
  }

  function handleLogout() {
    setMenuOpen(false);
    logout();
  }

  const isInstructor = user && (user.role === "Instructor" || user.role === "Admin");
  const isAdmin = user && user.role === "Admin";

  return (
    <header className="navbar">
      <Link to="/" className="logo">
        Smart<span>Learn</span>
      </Link>

      <NavLink to="/courses" className="nav-link hide-mobile">
        Explore
      </NavLink>

      <form className="nav-search" onSubmit={handleSearch}>
        <input
          type="text"
          placeholder="Search for anything"
          value={searchText}
          onChange={(e) => setSearchText(e.target.value)}
        />
      </form>

      <NavLink to="/plans" className="nav-link hide-mobile">
        Plans &amp; Pricing
      </NavLink>

      {isInstructor && (
        <NavLink to="/instructor" className="nav-link hide-mobile">
          Instructor
        </NavLink>
      )}

      {user && (
        <NavLink to="/my-learning" className="nav-link">
          My learning
        </NavLink>
      )}

      {!user && (
        <div className="nav-buttons">
          <Link to="/login" className="btn btn-outline">
            Log in
          </Link>
          <Link to="/register" className="btn btn-dark">
            Sign up
          </Link>
        </div>
      )}

      {user && (
        <div className="user-menu" onMouseLeave={() => setMenuOpen(false)}>
          <button className="avatar-button" onClick={() => setMenuOpen(!menuOpen)}>
            <Avatar name={user.fullName} imageUrl={user.profileImageUrl} size={36} />
          </button>

          {menuOpen && (
            <div className="dropdown" onClick={() => setMenuOpen(false)}>
              <div className="dropdown-header">
                <Avatar name={user.fullName} imageUrl={user.profileImageUrl} size={48} />
                <div>
                  <strong>{user.fullName}</strong>
                  <small>{user.email}</small>
                </div>
              </div>
              <Link to="/my-learning">My learning</Link>
              {isInstructor && <Link to="/instructor">Instructor dashboard</Link>}
              {isAdmin && <Link to="/admin">Admin panel</Link>}
              <Link to="/plans">Subscription</Link>
              <Link to="/purchases">Purchase history</Link>
              <Link to="/profile">Edit profile</Link>
              <button onClick={handleLogout}>Log out</button>
            </div>
          )}
        </div>
      )}
    </header>
  );
}

export default Navbar;
