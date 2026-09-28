import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { ArrowLeft, Award, GraduationCap, Lock, Printer } from "lucide-react";
import api, { getErrorMessage } from "../api";
import Spinner from "../components/Spinner";
import EmptyState from "../components/EmptyState";
import { formatDate, formatDuration } from "../utils";

// Certificate of completion. The "Print" button can also save it as a PDF.
function CertificatePage() {
  const { courseId } = useParams();
  const [certificate, setCertificate] = useState(null);
  const [error, setError] = useState("");

  useEffect(
    function () {
      api
        .get("/learn/" + courseId + "/certificate")
        .then(function (response) {
          setCertificate(response.data);
        })
        .catch(function (err) {
          setError(getErrorMessage(err));
        });
    },
    [courseId]
  );

  if (error) {
    return (
      <div className="container page">
        <EmptyState icon={Lock} title="Certificate not available yet" text={error}>
          <Link to={"/learn/" + courseId} className="btn btn-primary">
            Continue the course
          </Link>
        </EmptyState>
      </div>
    );
  }

  if (!certificate) {
    return <Spinner />;
  }

  return (
    <div className="container page">
      <div className="certificate-actions no-print">
        <Link to="/my-learning" className="back-link">
          <ArrowLeft size={16} /> My learning
        </Link>
        <button className="btn btn-primary" onClick={() => window.print()}>
          <Printer size={18} /> Print / Save as PDF
        </button>
      </div>

      <div className="certificate animate-in">
        <div className="certificate-inner">
          <div className="logo">
            <GraduationCap size={28} className="logo-icon" />
            <span className="logo-text">Smart<span className="logo-accent">Learn</span></span>
          </div>
          <p className="certificate-small">CERTIFICATE OF COMPLETION</p>
          <h1>{certificate.courseTitle}</h1>
          <p className="muted">Instructor: {certificate.instructorName}</p>
          <p className="certificate-small">This certificate is proudly awarded to</p>
          <h2 className="certificate-name">{certificate.studentName}</h2>
          <p>
            Completed on {formatDate(certificate.completedAt)} · {formatDuration(certificate.totalDurationSeconds)} of
            video
          </p>
          <div className="certificate-seal">
            <Award size={40} />
          </div>
          <p className="certificate-small">Certificate no: {certificate.certificateNumber}</p>
        </div>
      </div>
    </div>
  );
}

export default CertificatePage;
