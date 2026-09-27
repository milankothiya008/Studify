import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import api, { getErrorMessage } from "../api";
import Spinner from "../components/Spinner";
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
        <div className="empty-state">
          <h2>{error}</h2>
          <Link to={"/learn/" + courseId} className="btn btn-primary">
            Continue the course
          </Link>
        </div>
      </div>
    );
  }

  if (!certificate) {
    return <Spinner />;
  }

  return (
    <div className="container page">
      <div className="certificate-actions no-print">
        <Link to="/my-learning">‹ My learning</Link>
        <button className="btn btn-primary" onClick={() => window.print()}>
          Print / Save as PDF
        </button>
      </div>

      <div className="certificate">
        <div className="logo">
          Smart<span>Learn</span>
        </div>
        <p className="certificate-small">CERTIFICATE OF COMPLETION</p>
        <h1>{certificate.courseTitle}</h1>
        <p>Instructor: {certificate.instructorName}</p>
        <p className="certificate-small">This certificate is awarded to</p>
        <h2 className="certificate-name">{certificate.studentName}</h2>
        <p>
          on {formatDate(certificate.completedAt)} · {formatDuration(certificate.totalDurationSeconds)} of video
        </p>
        <p className="certificate-small">Certificate no: {certificate.certificateNumber}</p>
      </div>
    </div>
  );
}

export default CertificatePage;
