import { useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { useAuth } from "../AuthContext";
import { getErrorMessage } from "../api";

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
      const newUser = await register(fullName, email, password, role);
      if (newUser.role === "Instructor") {
        navigate("/instructor");
      } else {
        navigate("/courses");
      }
    } catch (err) {
      setError(getErrorMessage(err));
      setWorking(false);
    }
  }

  return (
    <div className="auth-page">
      <form className="auth-card" onSubmit={handleSubmit}>
        <h1>Sign up and start learning</h1>

        {error && <div className="alert alert-error">{error}</div>}

        <label>I want to</label>
        <div className="role-picker">
          <button
            type="button"
            className={role === "Student" ? "role-option selected" : "role-option"}
            onClick={() => setRole("Student")}
          >
            🎒 Learn
            <small>Student</small>
          </button>
          <button
            type="button"
            className={role === "Instructor" ? "role-option selected" : "role-option"}
            onClick={() => setRole("Instructor")}
          >
            🧑‍🏫 Teach
            <small>Instructor</small>
          </button>
        </div>

        <label>Full name</label>
        <input type="text" value={fullName} onChange={(e) => setFullName(e.target.value)} required />

        <label>Email</label>
        <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />

        <label>Password</label>
        <input
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          minLength={6}
          required
        />
        <p className="muted small">At least 6 characters.</p>

        <button type="submit" className="btn btn-primary btn-block" disabled={working}>
          {working ? "Creating account..." : "Sign up"}
        </button>

        <p className="center">
          Already have an account? <Link to="/login">Log in</Link>
        </p>
      </form>
    </div>
  );
}

export default RegisterPage;
