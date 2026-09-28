import { Link } from "react-router-dom";
import { Award, PlayCircle, TrendingUp } from "lucide-react";

// Shared layout for login, sign up, verify email and forgot password:
// a purple brand panel on the left and the form on the right.
function AuthLayout({ title, subtitle, children }) {
  return (
    <div className="auth-page">
      <aside className="auth-side">
        <div className="header-glow header-glow-1"></div>
        <div className="header-glow header-glow-2"></div>
        <div className="auth-side-content">
          <Link to="/" className="logo logo-light">
            <span className="logo-text">Smart<span className="logo-accent">Learn</span></span>
          </Link>
          <h2>Learn without limits</h2>
          <p>Build real skills with courses from expert instructors, at your own pace.</p>
          <ul className="auth-benefits">
            <li>
              <PlayCircle size={22} /> Video lessons, section by section
            </li>
            <li>
              <TrendingUp size={22} /> Progress tracking on every lecture
            </li>
            <li>
              <Award size={22} /> Certificates when you finish
            </li>
          </ul>
        </div>
      </aside>

      <div className="auth-main">
        <div className="auth-card animate-in">
          <h1>{title}</h1>
          {subtitle && <p className="auth-subtitle">{subtitle}</p>}
          {children}
        </div>
      </div>
    </div>
  );
}

export default AuthLayout;
