import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { AlertCircle, Check, EyeOff, Rocket, Trash2, X } from "lucide-react";
import api, { getErrorMessage } from "../../api";
import { useToast } from "../../ToastContext";

// The "Publish" tab: a checklist, publish / unpublish, and delete the course.
function PublishPanel({ course, onChanged }) {
  const navigate = useNavigate();
  const showToast = useToast();
  const [error, setError] = useState("");
  const [working, setWorking] = useState(false);

  let hasVideo = false;
  course.sections.forEach(function (section) {
    section.lectures.forEach(function (lecture) {
      if (lecture.hasVideo) {
        hasVideo = true;
      }
    });
  });

  // The same rules the backend checks before publishing.
  const checklist = [
    { label: "Subtitle", done: Boolean(course.subtitle) },
    { label: "Description", done: Boolean(course.description) },
    { label: "Category", done: Boolean(course.categoryId) },
    { label: "Course image", done: Boolean(course.thumbnailUrl) },
    { label: "At least one lecture with a video", done: hasVideo },
  ];
  const readyToPublish = checklist.every((item) => item.done);

  async function runAction(url) {
    setError("");
    setWorking(true);
    try {
      const response = await api.post(url);
      showToast(response.data.message);
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
      showToast("Course deleted.");
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
          Your course is a <span className="badge badge-warning">Draft</span>. Only you can see it until you publish it.
        </p>
      )}

      {!course.isPublished && (
        <ul className="publish-checklist">
          {checklist.map(function (item) {
            return (
              <li key={item.label} className={item.done ? "done" : ""}>
                <span className="checklist-icon">{item.done ? <Check size={14} /> : <X size={14} />}</span>
                {item.label}
              </li>
            );
          })}
        </ul>
      )}

      {error && (
        <div className="alert alert-error">
          <AlertCircle size={18} /> {error}
        </div>
      )}

      {course.isPublished ? (
        <button
          className="btn btn-dark"
          disabled={working}
          onClick={() => runAction("/instructor/courses/" + course.id + "/unpublish")}
        >
          <EyeOff size={18} /> Unpublish
        </button>
      ) : (
        <button
          className="btn btn-primary btn-large"
          disabled={working || !readyToPublish}
          onClick={() => runAction("/instructor/courses/" + course.id + "/publish")}
        >
          {working ? <span className="btn-spinner"></span> : <Rocket size={18} />} Publish course
        </button>
      )}

      <div className="danger-zone">
        <h3>Delete course</h3>
        <p className="muted small">You can delete a course only while no student is enrolled. Otherwise, unpublish it.</p>
        <button className="btn btn-danger" onClick={handleDelete}>
          <Trash2 size={18} /> Delete course
        </button>
      </div>
    </div>
  );
}

export default PublishPanel;
