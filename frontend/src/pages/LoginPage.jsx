import { useState } from "react";
import { Link, useLocation, useNavigate, useSearchParams } from "react-router-dom";
import { AlertCircle, Info } from "lucide-react";
import { useAuth } from "../AuthContext";
import { useToast } from "../ToastContext";
import { getErrorMessage } from "../api";
import AuthLayout from "../components/AuthLayout";
import PasswordInput from "../components/PasswordInput";

function LoginPage() {
  const { login } = useAuth();
  const showToast = useToast();
  const navigate = useNavigate();
  const location = useLocation();
  const [searchParams] = useSearchParams();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [working, setWorking] = useState(false);

  // The page the user wanted before being sent to login.
  let goBackTo = "/";
  if (location.state && location.state.from) {
    goBackTo = location.state.from;
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setError("");
    setWorking(true);
    try {
      const loggedInUser = await login(email, password);
      showToast("Welcome back, " + loggedInUser.fullName.split(" ")[0] + "!");
      navigate(goBackTo);
    } catch (err) {
      // The account exists but the email is not verified yet: go to the code page.
      const data = err.response && err.response.data;
      if (data && data.code === "EMAIL_NOT_VERIFIED") {
        navigate("/verify-email?email=" + encodeURIComponent(data.email), { state: { message: data.message } });
        return;
      }
      setError(getErrorMessage(err));
      setWorking(false);
    }
  }

  // Fills the form with one of the demo accounts created by the backend.
  function fillDemoAccount(demoEmail) {
    setEmail(demoEmail);
    setPassword("Password@123");
  }

  return (
    <AuthLayout title="Welcome back" subtitle="Log in to continue learning.">
      {searchParams.get("expired") && (
        <div className="alert alert-info">
          <Info size={18} /> Your session has ended. Please log in again.
        </div>
      )}

      {error && (
        <div className="alert alert-error">
          <AlertCircle size={18} /> {error}
        </div>
      )}

      <form onSubmit={handleSubmit}>
        <div className="form-field">
          <label htmlFor="email">Email</label>
          <input
            id="email"
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            autoComplete="email"
            required
          />
        </div>

        <div className="form-field">
          <div className="label-row">
            <label htmlFor="password">Password</label>
            <Link to="/forgot-password" className="small-link">
              Forgot password?
            </Link>
          </div>
          <PasswordInput value={password} onChange={(e) => setPassword(e.target.value)} autoComplete="current-password" />
        </div>

        <button type="submit" className="btn btn-primary btn-block btn-large" disabled={working}>
          {working && <span className="btn-spinner"></span>}
          {working ? "Logging in..." : "Log in"}
        </button>
      </form>

      <p className="auth-switch">
        Don't have an account? <Link to="/register">Sign up</Link>
      </p>

      {/* Demo buttons are shown only on your computer (npm run dev), never on the live site. */}
      {import.meta.env.DEV && (
        <div className="demo-accounts">
          <p>Demo accounts (password: Password@123)</p>
          <div>
            <button type="button" className="chip" onClick={() => fillDemoAccount("student@smartlearn.dev")}>
              Student
            </button>
            <button type="button" className="chip" onClick={() => fillDemoAccount("instructor@smartlearn.dev")}>
              Instructor
            </button>
            <button type="button" className="chip" onClick={() => fillDemoAccount("admin@smartlearn.dev")}>
              Admin
            </button>
          </div>
        </div>
      )}
    </AuthLayout>
  );
}

export default LoginPage;
