import { useState } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../AuthContext";
import { getErrorMessage } from "../api";

function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

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
      await login(email, password);
      navigate(goBackTo);
    } catch (err) {
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
    <div className="auth-page">
      <form className="auth-card" onSubmit={handleSubmit}>
        <h1>Log in to your account</h1>

        {error && <div className="alert alert-error">{error}</div>}

        <label>Email</label>
        <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />

        <label>Password</label>
        <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required />

        <button type="submit" className="btn btn-primary btn-block" disabled={working}>
          {working ? "Logging in..." : "Log in"}
        </button>

        <p className="center">
          Don't have an account? <Link to="/register">Sign up</Link>
        </p>

        <div className="demo-accounts">
          <p className="muted small">Demo accounts (password: Password@123)</p>
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
      </form>
    </div>
  );
}

export default LoginPage;
