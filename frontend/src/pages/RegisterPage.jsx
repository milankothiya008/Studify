import { useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { AlertCircle, Backpack, Presentation } from "lucide-react";
import { useAuth } from "../AuthContext";
import { getErrorMessage } from "../api";
import AuthLayout from "../components/AuthLayout";
import PasswordInput from "../components/PasswordInput";

function RegisterPage() {
  const { register } = useAuth();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  // /register?role=Instructor opens the form with "Instructor" already selected.
  const [role, setRole] = useState(searchParams.get("role") === "Instructor" ? "Instructor" : "Student");
  const [error, setError] = useState("");
  const [working, setWorking] = useState(false);

  async function handleSubmit(event) {
    event.preventDefault();
    setError("");
    setWorking(true);
    try {
      const result = await register(fullName, email, password, role);
      // Next step: type the 6-digit code we emailed.
      navigate("/verify-email?email=" + encodeURIComponent(result.email), { state: { message: result.message } });
    } catch (err) {
      setError(getErrorMessage(err));
      setWorking(false);
    }
  }

  return (
    <AuthLayout title="Create your account" subtitle="Join for free. It takes less than a minute.">
      {error && (
        <div className="alert alert-error">
          <AlertCircle size={18} /> {error}
        </div>
      )}

      <form onSubmit={handleSubmit}>
        <div className="form-field">
          <label>I want to</label>
          <div className="role-picker">
            <button
              type="button"
              className={role === "Student" ? "role-option selected" : "role-option"}
              onClick={() => setRole("Student")}
            >
              <Backpack size={24} />
              <strong>Learn</strong>
              <small>as a student</small>
            </button>
            <button
              type="button"
              className={role === "Instructor" ? "role-option selected" : "role-option"}
              onClick={() => setRole("Instructor")}
            >
              <Presentation size={24} />
              <strong>Teach</strong>
              <small>as an instructor</small>
            </button>
          </div>
        </div>

        <div className="form-field">
          <label htmlFor="fullName">Full name</label>
          <input
            id="fullName"
            type="text"
            value={fullName}
            onChange={(e) => setFullName(e.target.value)}
            autoComplete="name"
            required
          />
        </div>

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
          <p className="field-hint">We'll send a 6-digit code to verify it.</p>
        </div>

        <div className="form-field">
          <label>Password</label>
          <PasswordInput
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            minLength={6}
            autoComplete="new-password"
          />
          <p className="field-hint">At least 6 characters.</p>
        </div>

        <button type="submit" className="btn btn-primary btn-block btn-large" disabled={working}>
          {working && <span className="btn-spinner"></span>}
          {working ? "Creating account..." : "Create account"}
        </button>
      </form>

      <p className="auth-switch">
        Already have an account? <Link to="/login">Log in</Link>
      </p>
    </AuthLayout>
  );
}

export default RegisterPage;
