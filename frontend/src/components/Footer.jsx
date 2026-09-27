import { Link } from "react-router-dom";

function Footer() {
  return (
    <footer className="footer">
      <div className="container footer-inner">
        <div>
          <div className="logo logo-light">
            Smart<span>Learn</span>
          </div>
          <p>Learn anything, anytime. Built with ASP.NET Core and React.</p>
        </div>
        <div className="footer-links">
          <Link to="/courses">Explore courses</Link>
          <Link to="/plans">Plans &amp; Pricing</Link>
          <Link to="/register">Teach on SmartLearn</Link>
        </div>
      </div>
      <div className="container footer-bottom">© {new Date().getFullYear()} SmartLearn. Demo project.</div>
    </footer>
  );
}

export default Footer;
