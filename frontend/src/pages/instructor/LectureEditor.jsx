import { useState } from "react";
import { AlertCircle, ArrowDown, ArrowUp, Eye, EyeOff, Pencil, PlayCircle, Trash2, Upload } from "lucide-react";
import api, { getErrorMessage } from "../../api";
import { useToast } from "../../ToastContext";
import ProgressBar from "../../components/ProgressBar";
import { formatClock, getUploadPercent, getVideoDuration } from "../../utils";

// One lecture in the curriculum editor: edit, move, delete and upload its video.
function LectureEditor({ lecture, number, isFirst, isLast, onChanged }) {
  const showToast = useToast();
  const [isEditing, setIsEditing] = useState(false);
  const [title, setTitle] = useState(lecture.title);
  const [description, setDescription] = useState(lecture.description || "");
  const [isFreePreview, setIsFreePreview] = useState(lecture.isFreePreview);
  const [uploadProgress, setUploadProgress] = useState(null); // null = not uploading
  const [showVideo, setShowVideo] = useState(false);
  const [error, setError] = useState("");

  async function handleSave(event) {
    event.preventDefault();
    setError("");
    try {
      await api.put("/instructor/lectures/" + lecture.id, {
        title: title,
        description: description,
        isFreePreview: isFreePreview,
      });
      setIsEditing(false);
      showToast("Lecture saved.");
      onChanged();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  }

  function cancelEdit() {
    // Put back the saved values.
    setTitle(lecture.title);
    setDescription(lecture.description || "");
    setIsFreePreview(lecture.isFreePreview);
    setIsEditing(false);
  }

  async function handleMove(direction) {
    try {
      await api.post("/instructor/lectures/" + lecture.id + "/move?direction=" + direction);
      onChanged();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  }

  async function handleDelete() {
    if (!window.confirm('Delete lecture "' + lecture.title + '"?')) {
      return;
    }
    try {
      await api.delete("/instructor/lectures/" + lecture.id);
      showToast("Lecture deleted.");
      onChanged();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  }

  async function handleVideoChange(event) {
    const file = event.target.files[0];
    event.target.value = ""; // so choosing the same file again works
    if (!file) {
      return;
    }

    setError("");
    setUploadProgress(0);

    // Read the video length in the browser and send it along with the file.
    const durationSeconds = await getVideoDuration(file);

    const formData = new FormData();
    formData.append("file", file);
    formData.append("durationSeconds", durationSeconds);

    try {
      await api.post("/instructor/lectures/" + lecture.id + "/video", formData, {
        onUploadProgress: function (progressEvent) {
          setUploadProgress(getUploadPercent(progressEvent));
        },
      });
      showToast("Video uploaded.");
      onChanged();
    } catch (err) {
      setError(getErrorMessage(err));
    }
    setUploadProgress(null);
  }

  if (isEditing) {
    return (
      <form className="lecture-editor editing" onSubmit={handleSave}>
        <div className="form-field">
          <label>Lecture title</label>
          <input value={title} onChange={(e) => setTitle(e.target.value)} maxLength={200} autoFocus required />
        </div>

        <div className="form-field">
          <label>Description (optional)</label>
          <textarea rows={3} value={description} onChange={(e) => setDescription(e.target.value)} />
        </div>

        <label className="checkbox-label">
          <input type="checkbox" checked={isFreePreview} onChange={(e) => setIsFreePreview(e.target.checked)} />
          Free preview (anyone can watch this lecture)
        </label>

        {error && (
          <div className="alert alert-error">
            <AlertCircle size={18} /> {error}
          </div>
        )}

        <div className="card-actions">
          <button type="submit" className="btn btn-primary btn-small">
            Save lecture
          </button>
          <button type="button" className="btn btn-ghost btn-small" onClick={cancelEdit}>
            Cancel
          </button>
        </div>
      </form>
    );
  }

  return (
    <div className="lecture-editor">
      <div className="lecture-editor-row">
        <div className="lecture-editor-title">
          <PlayCircle size={18} className="muted" />
          <span>
            <span className="muted">Lecture {number}:</span> {lecture.title}
          </span>
          {lecture.isFreePreview && <span className="badge badge-brand">Free preview</span>}
          {lecture.hasVideo ? (
            <span className="badge badge-success">Video · {formatClock(lecture.durationSeconds)}</span>
          ) : (
            <span className="badge badge-warning">No video</span>
          )}
        </div>

        <div className="icon-buttons">
          <button title="Move up" disabled={isFirst} onClick={() => handleMove("up")}>
            <ArrowUp size={16} />
          </button>
          <button title="Move down" disabled={isLast} onClick={() => handleMove("down")}>
            <ArrowDown size={16} />
          </button>
          <button title="Edit" onClick={() => setIsEditing(true)}>
            <Pencil size={16} />
          </button>
          <button title="Delete" className="danger" onClick={handleDelete}>
            <Trash2 size={16} />
          </button>
        </div>
      </div>

      {error && (
        <div className="alert alert-error">
          <AlertCircle size={18} /> {error}
        </div>
      )}

      {uploadProgress !== null ? (
        <div className="upload-status">
          <ProgressBar percent={uploadProgress} />
          <small>
            {uploadProgress < 100 ? "Uploading " + uploadProgress + "%" : "Processing video, please wait..."}
          </small>
        </div>
      ) : (
        <div className="lecture-editor-actions">
          <label className="btn btn-outline btn-small">
            <Upload size={15} /> {lecture.hasVideo ? "Replace video" : "Upload video"}
            <input type="file" accept="video/*" hidden onChange={handleVideoChange} />
          </label>
          {lecture.videoUrl && (
            <button className="link-button" onClick={() => setShowVideo(!showVideo)}>
              {showVideo ? <EyeOff size={15} /> : <Eye size={15} />} {showVideo ? "Hide video" : "Watch video"}
            </button>
          )}
        </div>
      )}

      {showVideo && lecture.videoUrl && (
        <video className="lecture-editor-video" src={lecture.videoUrl} controls preload="metadata" />
      )}
    </div>
  );
}

export default LectureEditor;
