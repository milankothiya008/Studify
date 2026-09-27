import { Link } from "react-router-dom";

function NotFoundPage() {
  return (
    <div className="container page">
      <div className="empty-state">
        <h1>404</h1>
        <p>We can't find the page you're looking for.</p>
        <Link to="/" className="btn btn-primary">
          Go home
        </Link>
      </div>
    </div>
  );
}

export default NotFoundPage;
