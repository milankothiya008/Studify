import { Link } from "react-router-dom";
import { GraduationCap } from "lucide-react";

function Footer() {
  return (
    <footer className="footer">
      <div className="container footer-grid">
        <div className="footer-brand">
          <div className="logo logo-light">
            <GraduationCap size={28} className="logo-icon" />
            <span className="logo-text">Smart<span className="logo-accent">Learn</span></span>
          </div>
          <p>Learn anything, anytime. Video courses from expert instructors, with progress tracking and certificates.</p>
        </div>

        <div className="footer-column">
          <h4>Learn</h4>
          <Link to="/courses">Explore courses</Link>
          <Link to="/plans">Plans &amp; Pricing</Link>
          <Link to="/my-learning">My learning</Link>
        </div>

        <div className="footer-column">
          <h4>Teach</h4>
          <Link to="/register?role=Instructor">Become an instructor</Link>
          <Link to="/instructor">Instructor dashboard</Link>
        </div>

        <div className="footer-column">
          <h4>Account</h4>
          <Link to="/profile">Account settings</Link>
          <Link to="/purchases">Purchase history</Link>
          <Link to="/forgot-password">Forgot password</Link>
        </div>
      </div>

      <div className="container footer-bottom">
        <span>© {new Date().getFullYear()} SmartLearn</span>
        <span>Built with ASP.NET Core &amp; React</span>
      </div>
    </footer>
  );
}

export default Footer;
