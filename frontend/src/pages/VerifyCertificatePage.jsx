import { useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { BadgeCheck, Search, ShieldX } from "lucide-react";
import api from "../api";
import PageHeader from "../components/PageHeader";
import Spinner from "../components/Spinner";
import { formatDate, formatDuration } from "../utils";

// Public page: anybody (for example an employer) can check that a certificate is real.
// /verify            -> a form to type the certificate number
// /verify/SL-1-3-2   -> the result
function VerifyCertificatePage() {
  const { number } = useParams();
  const navigate = useNavigate();
  const [typed, setTyped] = useState(number || "");
  const [result, setResult] = useState(null);
  const [loading, setLoading] = useState(false);

  useEffect(
    function () {
      if (!number) {
        setResult(null);
        return;
      }
      setLoading(true);
      api
        .get("/certificates/" + encodeURIComponent(number))
        .then(function (response) {
          setResult(response.data);
        })
        .catch(function () {
          setResult({ isValid: false, certificateNumber: number });
        })
        .finally(function () {
          setLoading(false);
        });
    },
    [number]
  );

  function handleSubmit(event) {
    event.preventDefault();
    if (typed.trim()) {
      navigate("/verify/" + encodeURIComponent(typed.trim()));
    }
  }

  return (
    <div>
      <PageHeader
        center
        title="Verify a certificate"
        subtitle="Check that a SmartLearn certificate is real, and see who earned it."
      />

      <div className="container page narrow">
        <form className="verify-form card card-padded" onSubmit={handleSubmit}>
          <label htmlFor="number">Certificate number</label>
          <div className="add-row">
            <input
              id="number"
              placeholder="e.g. SL-1-3-2"
              value={typed}
              onChange={(e) => setTyped(e.target.value)}
              required
            />
            <button type="submit" className="btn btn-primary">
              <Search size={18} /> Verify
            </button>
          </div>
        </form>

        {loading && <Spinner text="Checking..." />}

        {!loading && result && result.isValid && (
          <div className="verify-result valid animate-in">
            <div className="verify-icon">
              <BadgeCheck size={40} />
            </div>
            <h2>Valid certificate</h2>
            <p>
              <strong>{result.studentName}</strong> completed{" "}
              <Link to={"/course/" + result.courseId}>{result.courseTitle}</Link>
            </p>
            <dl className="verify-details">
              <div>
                <dt>Instructor</dt>
                <dd>{result.instructorName}</dd>
              </div>
              <div>
                <dt>Completed on</dt>
                <dd>{formatDate(result.completedAt)}</dd>
              </div>
              <div>
                <dt>Course length</dt>
                <dd>{formatDuration(result.totalDurationSeconds)} of video</dd>
              </div>
              <div>
                <dt>Certificate no.</dt>
                <dd className="mono">{result.certificateNumber}</dd>
              </div>
            </dl>
          </div>
        )}

        {!loading && result && !result.isValid && (
          <div className="verify-result invalid animate-in">
            <div className="verify-icon">
              <ShieldX size={40} />
            </div>
            <h2>No certificate found</h2>
            <p className="muted">
              There is no SmartLearn certificate with the number <span className="mono">{result.certificateNumber}</span>.
              Check the number and try again.
            </p>
          </div>
        )}
      </div>
    </div>
  );
}

export default VerifyCertificatePage;
