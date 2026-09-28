import { useEffect, useState } from "react";
import { Link, useLocation, useNavigate, useSearchParams } from "react-router-dom";
import { AlertCircle, MailCheck } from "lucide-react";
import api, { getErrorMessage } from "../api";
import { useAuth } from "../AuthContext";
import { useToast } from "../ToastContext";
import AuthLayout from "../components/AuthLayout";
import OtpInput from "../components/OtpInput";

// Seconds to wait before "Resend code" can be used again (same as the backend).
const RESEND_WAIT = 60;

// Step 2 of sign up: type the 6-digit code that was emailed.
function VerifyEmailPage() {
  const { verifyEmail } = useAuth();
  const showToast = useToast();
  const navigate = useNavigate();
  const location = useLocation();
  const [searchParams] = useSearchParams();
  const email = searchParams.get("email") || "";

  const [code, setCode] = useState("");
  const [error, setError] = useState("");
  const [info, setInfo] = useState(location.state && location.state.message ? location.state.message : "");
  const [working, setWorking] = useState(false);
  const [secondsLeft, setSecondsLeft] = useState(RESEND_WAIT); // a code was just sent

  // Count down once per second until "Resend code" is available.
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

  async function verify(codeToCheck) {
    if (codeToCheck.length !== 6 || working) {
      return;
    }
    setError("");
    setWorking(true);
    try {
      const user = await verifyEmail(email, codeToCheck);
      showToast("Email verified. Welcome to SmartLearn, " + user.fullName.split(" ")[0] + "!");
      navigate(user.role === "Instructor" ? "/instructor" : "/courses");
    } catch (err) {
      setError(getErrorMessage(err));
      setWorking(false);
    }
  }

  function handleSubmit(event) {
    event.preventDefault();
    verify(code);
  }

  async function handleResend() {
    setError("");
    setInfo("");
    try {
      const response = await api.post("/auth/resend-code", { email: email });
      setInfo(response.data.message);
      setSecondsLeft(RESEND_WAIT);
    } catch (err) {
      setError(getErrorMessage(err));
    }
  }

  if (!email) {
    return (
      <AuthLayout title="Verify your email">
        <p>
          Something is missing. Please <Link to="/register">sign up</Link> or <Link to="/login">log in</Link> again.
        </p>
      </AuthLayout>
    );
  }

  return (
    <AuthLayout title="Check your inbox" subtitle="Enter the 6-digit code we sent to verify your email.">
      <div className="email-pill">
        <MailCheck size={18} /> {email}
      </div>

      {info && !error && <div className="alert alert-info">{info}</div>}
      {error && (
        <div className="alert alert-error">
          <AlertCircle size={18} /> {error}
        </div>
      )}

      <form onSubmit={handleSubmit}>
        <OtpInput onChange={setCode} onComplete={verify} disabled={working} />

        <button type="submit" className="btn btn-primary btn-block btn-large" disabled={working || code.length !== 6}>
          {working && <span className="btn-spinner"></span>}
          {working ? "Verifying..." : "Verify email"}
        </button>
      </form>

      <p className="auth-switch">
        Didn't get it? Check your spam folder, or{" "}
        {secondsLeft > 0 ? (
          <span className="muted">resend in {secondsLeft}s</span>
        ) : (
          <button type="button" className="link-button" onClick={handleResend}>
            resend the code
          </button>
        )}
      </p>
      <p className="auth-switch">
        Wrong email? <Link to="/register">Sign up again</Link>
      </p>
    </AuthLayout>
  );
}

export default VerifyEmailPage;
