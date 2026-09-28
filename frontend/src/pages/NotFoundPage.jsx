import { Link } from "react-router-dom";
import { Compass, Home } from "lucide-react";

function NotFoundPage() {
  return (
    <div className="container page">
      <div className="not-found animate-in">
        <div className="not-found-code">404</div>
        <h1>This page wandered off</h1>
        <p className="muted">We can't find the page you're looking for.</p>
        <div className="card-actions center-actions">
          <Link to="/" className="btn btn-primary">
            <Home size={18} /> Go home
          </Link>
          <Link to="/courses" className="btn btn-outline">
            <Compass size={18} /> Explore courses
          </Link>
        </div>
      </div>
    </div>
  );
}

export default NotFoundPage;
