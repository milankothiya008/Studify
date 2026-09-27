import { useState } from "react";
import api, { getErrorMessage } from "../../api";
import ProgressBar from "../../components/ProgressBar";
import { formatClock, getVideoDuration } from "../../utils";

// One lecture in the curriculum editor: edit, move, delete and upload its video.
function LectureEditor({ lecture, number, isFirst, isLast, onChanged }) {
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
      onChanged();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  }

  async function handleMove(direction) {
    await api.post("/instructor/lectures/" + lecture.id + "/move?direction=" + direction);
    onChanged();
  }

  async function handleDelete() {
    if (!window.confirm('Delete lecture "' + lecture.title + '"?')) {
      return;
    }
    try {
      await api.delete("/instructor/lectures/" + lecture.id);
      onChanged();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  }

  async function handleVideoChange(event) {
    const file = event.target.files[0];
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
          setUploadProgress(Math.round((progressEvent.loaded * 100) / progressEvent.total));
        },
      });
      onChanged();
    } catch (err) {
      setError(getErrorMessage(err));
    }
    setUploadProgress(null);
  }

  if (isEditing) {
    return (
      <form className="lecture-editor editing" onSubmit={handleSave}>
        <label>Lecture title</label>
        <input value={title} onChange={(e) => setTitle(e.target.value)} required />

        <label>Description (optional)</label>
        <textarea rows={3} value={description} onChange={(e) => setDescription(e.target.value)} />

        <label className="checkbox-label">
          <input type="checkbox" checked={isFreePreview} onChange={(e) => setIsFreePreview(e.target.checked)} />
          Free preview (anyone can watch this lecture)
        </label>

        {error && <div className="alert alert-error">{error}</div>}

        <div className="card-actions">
          <button type="submit" className="btn btn-primary btn-small">
            Save lecture
          </button>
          <button type="button" className="btn btn-outline btn-small" onClick={() => setIsEditing(false)}>
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
          <span>
            ▶ Lecture {number}: {lecture.title}
          </span>
          {lecture.isFreePreview && <span className="badge">Free preview</span>}
          {lecture.hasVideo ? (
            <span className="badge badge-success">Video · {formatClock(lecture.durationSeconds)}</span>
          ) : (
            <span className="badge badge-warning">No video</span>
          )}
        </div>

        <div className="icon-buttons">
          <button title="Move up" disabled={isFirst} onClick={() => handleMove("up")}>
            ↑
          </button>
          <button title="Move down" disabled={isLast} onClick={() => handleMove("down")}>
            ↓
          </button>
          <button title="Edit" onClick={() => setIsEditing(true)}>
            ✎
          </button>
          <button title="Delete" onClick={handleDelete}>
            🗑
          </button>
        </div>
      </div>

      {error && <div className="alert alert-error">{error}</div>}

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
            {lecture.hasVideo ? "Replace video" : "Upload video"}
            <input type="file" accept="video/*" hidden onChange={handleVideoChange} />
          </label>
          {lecture.videoUrl && (
            <button className="link-button" onClick={() => setShowVideo(!showVideo)}>
              {showVideo ? "Hide video" : "Watch video"}
            </button>
          )}
        </div>
      )}

      {showVideo && lecture.videoUrl && <video className="lecture-editor-video" src={lecture.videoUrl} controls />}
    </div>
  );
}

export default LectureEditor;
