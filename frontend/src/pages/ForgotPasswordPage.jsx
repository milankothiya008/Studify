import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { AlertCircle, ArrowLeft, MailCheck } from "lucide-react";
import api, { getErrorMessage } from "../api";
import { useAuth } from "../AuthContext";
import { useToast } from "../ToastContext";
import AuthLayout from "../components/AuthLayout";
import OtpInput from "../components/OtpInput";
import PasswordInput from "../components/PasswordInput";

const RESEND_WAIT = 60;

// Forgot password in two steps:
//   step "email": type your email -> we send a 6-digit code
//   step "reset": type the code and a new password
function ForgotPasswordPage() {
  const { resetPassword } = useAuth();
  const showToast = useToast();
  const navigate = useNavigate();

  const [step, setStep] = useState("email");
  const [email, setEmail] = useState("");
  const [code, setCode] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [error, setError] = useState("");
  const [info, setInfo] = useState("");
  const [working, setWorking] = useState(false);
  const [secondsLeft, setSecondsLeft] = useState(0);

  useEffect(
    function () {
      if (secondsLeft <= 0) {
        return;
      }
      const timer = setTimeout(function () {
        setSecondsLeft(secondsLeft - 1);
      }, 1000);
      return function () {
        clearTimeout(timer);
      };
    },
    [secondsLeft]
  );

  async function sendCode() {
    setError("");
    setWorking(true);
    try {
      const response = await api.post("/auth/forgot-password", { email: email });
      setInfo(response.data.message);
      setStep("reset");
      setSecondsLeft(RESEND_WAIT);
    } catch (err) {
      setError(getErrorMessage(err));
    }
    setWorking(false);
  }

  function handleEmailSubmit(event) {
    event.preventDefault();
    sendCode();
  }

  async function handleResetSubmit(event) {
    event.preventDefault();
    setError("");

    if (newPassword !== confirmPassword) {
      setError("The two passwords are not the same.");
      return;
    }

    setWorking(true);
    try {
      await resetPassword(email, code, newPassword);
      showToast("Your password has been changed. You are logged in.");
      navigate("/");
    } catch (err) {
      setError(getErrorMessage(err));
      setWorking(false);
    }
  }

  const errorBox = error && (
    <div className="alert alert-error">
      <AlertCircle size={18} /> {error}
    </div>
  );

  if (step === "email") {
    return (
      <AuthLayout title="Forgot your password?" subtitle="Enter your email and we'll send you a code to reset it.">
        {errorBox}
        <form onSubmit={handleEmailSubmit}>
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
          <button type="submit" className="btn btn-primary btn-block btn-large" disabled={working}>
            {working && <span className="btn-spinner"></span>}
            {working ? "Sending..." : "Send reset code"}
          </button>
        </form>
        <p className="auth-switch">
          <Link to="/login" className="back-link">
            <ArrowLeft size={16} /> Back to log in
          </Link>
        </p>
      </AuthLayout>
    );
  }

  return (
    <AuthLayout title="Choose a new password" subtitle="Enter the 6-digit code from the email and your new password.">
      <div className="email-pill">
        <MailCheck size={18} /> {email}
      </div>

      {info && !error && <div className="alert alert-info">{info}</div>}
      {errorBox}

      <form onSubmit={handleResetSubmit}>
        <div className="form-field">
          <label>Code</label>
          <OtpInput onChange={setCode} disabled={working} />
        </div>

        <div className="form-field">
          <label>New password</label>
          <PasswordInput
            value={newPassword}
            onChange={(e) => setNewPassword(e.target.value)}
            minLength={8}
            autoComplete="new-password"
          />
        </div>

        <div className="form-field">
          <label>Confirm new password</label>
          <PasswordInput
            value={confirmPassword}
            onChange={(e) => setConfirmPassword(e.target.value)}
            minLength={8}
            autoComplete="new-password"
          />
        </div>

        <button type="submit" className="btn btn-primary btn-block btn-large" disabled={working || code.length !== 6}>
          {working && <span className="btn-spinner"></span>}
          {working ? "Saving..." : "Reset password"}
        </button>
      </form>

      <p className="auth-switch">
        {secondsLeft > 0 ? (
          <span className="muted">You can ask for a new code in {secondsLeft}s</span>
        ) : (
          <button type="button" className="link-button" onClick={sendCode}>
            Send a new code
          </button>
        )}
        {" · "}
        <button type="button" className="link-button" onClick={() => setStep("email")}>
          Use another email
        </button>
      </p>
    </AuthLayout>
  );
}

export default ForgotPasswordPage;
