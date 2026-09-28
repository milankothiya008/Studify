import { Navigate, useLocation } from "react-router-dom";
import { ShieldAlert } from "lucide-react";
import { useAuth } from "../AuthContext";
import Spinner from "./Spinner";
import EmptyState from "./EmptyState";

// Wrap a page with this to allow only logged in users (and optionally only some roles).
// Example: <ProtectedRoute roles={["Admin"]}><AdminPage /></ProtectedRoute>
function ProtectedRoute({ children, roles }) {
  const { user, loading } = useAuth();
  const location = useLocation();

  if (loading) {
    return <Spinner />;
  }

  if (!user) {
    // Go to login, and come back to this page after logging in.
    return <Navigate to="/login" state={{ from: location.pathname }} replace />;
  }

  if (roles && !roles.includes(user.role)) {
    return (
      <div className="container page">
        <EmptyState
          icon={ShieldAlert}
          title="Access denied"
          text={"Your account (" + user.role + ") cannot open this page."}
        />
      </div>
    );
  }

  return children;
}

export default ProtectedRoute;
