import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { ArrowLeft, Award, Copy, ExternalLink, GraduationCap, Lock, Share2, Printer, ShieldCheck } from "lucide-react";
import api, { getErrorMessage } from "../api";
import { useToast } from "../ToastContext";
import Spinner from "../components/Spinner";
import EmptyState from "../components/EmptyState";
import { formatDate, formatDuration } from "../utils";

// Certificate of completion. The "Print" button can also save it as a PDF.
function CertificatePage() {
  const { courseId } = useParams();
  const showToast = useToast();
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

  // Public page where anybody can check this certificate.
  const verifyUrl = window.location.origin + "/verify/" + certificate.certificateNumber;

  // LinkedIn's "Add license or certification" form, filled in for the student.
  const completed = new Date(certificate.completedAt);
  const linkedInUrl =
    "https://www.linkedin.com/profile/add?startTask=CERTIFICATION_NAME" +
    "&name=" + encodeURIComponent(certificate.courseTitle) +
    "&organizationName=" + encodeURIComponent("SmartLearn") +
    "&issueYear=" + completed.getFullYear() +
    "&issueMonth=" + (completed.getMonth() + 1) +
    "&certUrl=" + encodeURIComponent(verifyUrl) +
    "&certId=" + encodeURIComponent(certificate.certificateNumber);

  async function copyLink() {
    try {
      await navigator.clipboard.writeText(verifyUrl);
      showToast("Verification link copied.");
    } catch (err) {
      showToast("Could not copy. The link is: " + verifyUrl, "info");
    }
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
          <p className="certificate-verify">Verify at {verifyUrl}</p>
        </div>
      </div>

      <div className="share-card card card-padded no-print">
        <div className="share-text">
          <ShieldCheck size={24} />
          <div>
            <strong>Share your achievement</strong>
            <p className="muted small">
              Anyone can confirm this certificate is real with the verification link.
            </p>
          </div>
        </div>
        <div className="card-actions">
          <a href={linkedInUrl} target="_blank" rel="noreferrer" className="btn btn-linkedin">
            <Share2 size={18} /> Add to LinkedIn
          </a>
          <button className="btn btn-outline" onClick={copyLink}>
            <Copy size={18} /> Copy verification link
          </button>
          <Link to={"/verify/" + certificate.certificateNumber} className="btn btn-ghost">
            <ExternalLink size={18} /> Open verification page
          </Link>
        </div>
      </div>
    </div>
  );
}

export default CertificatePage;
