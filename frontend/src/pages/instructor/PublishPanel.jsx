import { useState } from "react";
import { useNavigate } from "react-router-dom";
import api, { getErrorMessage } from "../../api";

// The "Publish" tab: publish, unpublish or delete the course.
function PublishPanel({ course, onChanged }) {
  const navigate = useNavigate();
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const [working, setWorking] = useState(false);

  async function runAction(url) {
    setMessage("");
    setError("");
    setWorking(true);
    try {
      const response = await api.post(url);
      setMessage(response.data.message);
      onChanged();
    } catch (err) {
      setError(getErrorMessage(err));
    }
    setWorking(false);
  }

  async function handleDelete() {
    if (!window.confirm("Delete this course and all its videos? This cannot be undone.")) {
      return;
    }
    try {
      await api.delete("/instructor/courses/" + course.id);
      navigate("/instructor");
    } catch (err) {
      setError(getErrorMessage(err));
    }
  }

  return (
    <div>
      <h2>Publish</h2>

      {course.isPublished ? (
        <p>
          Your course is <span className="badge badge-success">Published</span>. Students can find it, enroll and buy it.
        </p>
      ) : (
        <p>
          Your course is a <span className="badge badge-warning">Draft</span>. Only you can see it. Before publishing, add
          a subtitle, description, category, course image and at least one lecture with a video.
        </p>
      )}

      {error && <div className="alert alert-error">{error}</div>}
      {message && <div className="alert alert-success">{message}</div>}

      {course.isPublished ? (
        <button
          className="btn btn-dark"
          disabled={working}
          onClick={() => runAction("/instructor/courses/" + course.id + "/unpublish")}
        >
          Unpublish
        </button>
      ) : (
        <button
          className="btn btn-primary btn-large"
          disabled={working}
          onClick={() => runAction("/instructor/courses/" + course.id + "/publish")}
        >
          Publish course
        </button>
      )}

      <div className="danger-zone">
        <h3>Delete course</h3>
        <p className="muted small">
          You can delete a course only while no student is enrolled. Otherwise, unpublish it.
        </p>
        <button className="btn btn-danger" onClick={handleDelete}>
          Delete course
        </button>
      </div>
    </div>
  );
}

export default PublishPanel;
