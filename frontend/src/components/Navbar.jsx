import { useEffect, useRef, useState } from "react";
import { Link, NavLink, useLocation, useNavigate } from "react-router-dom";
import {
  BookOpen,
  ChevronDown,
  Compass,
  CreditCard,
  GraduationCap,
  Heart,
  LayoutDashboard,
  MessageCircleQuestion,
  LogOut,
  Menu,
  Receipt,
  Search,
  Shield,
  User,
  UserCheck,
  UserRound,
  X,
} from "lucide-react";
import { useAuth } from "../AuthContext";
import Avatar from "./Avatar";
import NotificationBell from "./NotificationBell";

function Navbar() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const [searchText, setSearchText] = useState("");
  const [menuOpen, setMenuOpen] = useState(false); // the user dropdown
  const [drawerOpen, setDrawerOpen] = useState(false); // the mobile menu
  const [scrolled, setScrolled] = useState(false);
  const menuRef = useRef(null);

  // Close the menus when the page changes.
  useEffect(
    function () {
      setMenuOpen(false);
      setDrawerOpen(false);
    },
    [location.pathname]
  );

  // Close the dropdown when the user clicks or taps anywhere outside it.
  useEffect(function () {
    function handlePointerDown(event) {
      if (menuRef.current && !menuRef.current.contains(event.target)) {
        setMenuOpen(false);
      }
    }
    document.addEventListener("pointerdown", handlePointerDown);
    return function () {
      document.removeEventListener("pointerdown", handlePointerDown);
    };
  }, []);

  // Add a shadow under the navbar once the page is scrolled.
  useEffect(function () {
    function handleScroll() {
      setScrolled(window.scrollY > 8);
    }
    window.addEventListener("scroll", handleScroll);
    return function () {
      window.removeEventListener("scroll", handleScroll);
    };
  }, []);

  function handleSearch(event) {
    event.preventDefault();
    navigate("/courses?search=" + encodeURIComponent(searchText.trim()));
  }

  const isInstructor = user && (user.role === "Instructor" || user.role === "Admin");
  const isAdmin = user && user.role === "Admin";

  // The course player has its own top bar, so the navbar is hidden there.
  if (location.pathname.startsWith("/learn/")) {
    return null;
  }

  return (
    <>
      <header className={scrolled ? "navbar scrolled" : "navbar"}>
        <div className="navbar-inner">
          <Link to="/" className="logo">
            <GraduationCap size={28} className="logo-icon" />
            <span className="logo-text">Smart<span className="logo-accent">Learn</span></span>
          </Link>

          <NavLink to="/courses" className="nav-link hide-mobile">
            <Compass size={18} /> Explore
          </NavLink>

          <form className="nav-search hide-mobile" onSubmit={handleSearch}>
            <Search size={18} />
            <input
              type="text"
              placeholder="Search for anything"
              value={searchText}
              onChange={(e) => setSearchText(e.target.value)}
            />
          </form>

          <div className="nav-right">
            <NavLink to="/plans" className="nav-link hide-mobile">
              Plans &amp; Pricing
            </NavLink>

            {isInstructor ? (
              <NavLink to="/instructor" className="nav-link hide-mobile">
                Instructor
              </NavLink>
            ) : (
              !user && (
                <NavLink to="/register?role=Instructor" className="nav-link hide-mobile">
                  Teach
                </NavLink>
              )
            )}

            {user && (
              <NavLink to="/my-learning" className="nav-link hide-mobile">
                My learning
              </NavLink>
            )}

            {user && (
              <NavLink to="/wishlist" className="icon-button nav-icon hide-mobile" title="Wishlist" aria-label="Wishlist">
                <Heart size={21} />
              </NavLink>
            )}

            {user && <NotificationBell />}

            {!user && (
              <div className="nav-buttons hide-mobile">
                <Link to="/login" className="btn btn-outline btn-small">
                  Log in
                </Link>
                <Link to="/register" className="btn btn-primary btn-small">
                  Sign up
                </Link>
              </div>
            )}

            {user && (
              <div className="user-menu" ref={menuRef}>
                <button
                  className="avatar-button"
                  onClick={() => setMenuOpen(!menuOpen)}
                  aria-label="Account menu"
                  aria-expanded={menuOpen}
                >
                  <Avatar name={user.fullName} imageUrl={user.profileImageUrl} size={38} />
                  <ChevronDown size={16} className={menuOpen ? "chevron open" : "chevron"} />
                </button>

                {menuOpen && (
                  <div className="dropdown">
                    <div className="dropdown-header">
                      <Avatar name={user.fullName} imageUrl={user.profileImageUrl} size={44} />
                      <div>
                        <strong>{user.fullName}</strong>
                        <small>{user.email}</small>
                      </div>
                    </div>
                    <Link to="/my-learning">
                      <BookOpen size={18} /> My learning
                    </Link>
                    <Link to="/wishlist">
                      <Heart size={18} /> Wishlist
                    </Link>
                    <Link to="/following">
                      <UserCheck size={18} /> Following
                    </Link>
                    {isInstructor && (
                      <Link to="/instructor">
                        <LayoutDashboard size={18} /> Instructor dashboard
                      </Link>
                    )}
                    {user.role === "Instructor" && (
                      <Link to={"/instructors/" + user.id}>
                        <UserRound size={18} /> My public profile
                      </Link>
                    )}
                    {isInstructor && (
                      <Link to="/instructor/questions">
                        <MessageCircleQuestion size={18} /> Student questions
                      </Link>
                    )}
                    {isAdmin && (
                      <Link to="/admin">
                        <Shield size={18} /> Admin panel
                      </Link>
                    )}
                    <Link to="/plans">
                      <CreditCard size={18} /> Subscription
                    </Link>
                    <Link to="/purchases">
                      <Receipt size={18} /> Purchase history
                    </Link>
                    <Link to="/profile">
                      <User size={18} /> Account settings
                    </Link>
                    <div className="dropdown-divider"></div>
                    <button onClick={logout}>
                      <LogOut size={18} /> Log out
                    </button>
                  </div>
                )}
              </div>
            )}

            <button className="menu-button show-mobile" onClick={() => setDrawerOpen(true)} aria-label="Open menu">
              <Menu size={24} />
            </button>
          </div>
        </div>

      </header>

        {/* Mobile menu that slides in from the right.
            It is outside <header> on purpose: the navbar's blur effect would trap it inside the navbar. */}
        {drawerOpen && (
          <div className="drawer-backdrop" onClick={() => setDrawerOpen(false)}>
            <nav className="drawer" onClick={(e) => e.stopPropagation()}>
              <div className="drawer-header">
                <span className="logo">
                  <span className="logo-text">Smart<span className="logo-accent">Learn</span></span>
                </span>
                <button className="icon-button" onClick={() => setDrawerOpen(false)} aria-label="Close menu">
                  <X size={22} />
                </button>
              </div>

              <form className="nav-search" onSubmit={handleSearch}>
                <Search size={18} />
                <input
                  type="text"
                  placeholder="Search for anything"
                  value={searchText}
                  onChange={(e) => setSearchText(e.target.value)}
                />
              </form>

              <Link to="/courses">
                <Compass size={20} /> Explore courses
              </Link>
              <Link to="/plans">
                <CreditCard size={20} /> Plans &amp; Pricing
              </Link>
              {user && (
                <Link to="/my-learning">
                  <BookOpen size={20} /> My learning
                </Link>
              )}
              {user && (
                <Link to="/wishlist">
                  <Heart size={20} /> Wishlist
                </Link>
              )}
              {isInstructor && (
                <Link to="/instructor">
                  <LayoutDashboard size={20} /> Instructor dashboard
                </Link>
              )}

              {!user && (
                <div className="drawer-buttons">
                  <Link to="/login" className="btn btn-outline btn-block">
                    Log in
                  </Link>
                  <Link to="/register" className="btn btn-primary btn-block">
                    Sign up
                  </Link>
                </div>
              )}
            </nav>
          </div>
        )}
    </>
  );
}

export default Navbar;
