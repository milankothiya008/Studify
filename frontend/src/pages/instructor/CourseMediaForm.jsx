import { useState } from "react";
import api, { getErrorMessage } from "../../api";
import ProgressBar from "../../components/ProgressBar";

// The "Image & promo video" tab.
function CourseMediaForm({ course, onChanged }) {
  const [imageUploading, setImageUploading] = useState(false);
  const [videoProgress, setVideoProgress] = useState(null); // null = not uploading
  const [error, setError] = useState("");

  async function handleImageChange(event) {
    const file = event.target.files[0];
    if (!file) {
      return;
    }

    const formData = new FormData();
    formData.append("file", file);

    setError("");
    setImageUploading(true);
    try {
      await api.post("/instructor/courses/" + course.id + "/thumbnail", formData);
      onChanged();
    } catch (err) {
      setError(getErrorMessage(err));
    }
    setImageUploading(false);
  }

  async function handleVideoChange(event) {
    const file = event.target.files[0];
    if (!file) {
      return;
    }

    const formData = new FormData();
    formData.append("file", file);

    setError("");
    setVideoProgress(0);
    try {
      await api.post("/instructor/courses/" + course.id + "/promo-video", formData, {
        // Called many times while the file is being sent, so we can show a progress bar.
        onUploadProgress: function (progressEvent) {
          setVideoProgress(Math.round((progressEvent.loaded * 100) / progressEvent.total));
        },
      });
      onChanged();
    } catch (err) {
      setError(getErrorMessage(err));
    }
    setVideoProgress(null);
  }

  return (
    <div>
      <h2>Image & promo video</h2>

      {error && <div className="alert alert-error">{error}</div>}

      <div className="media-block">
        <div className="media-preview">
          {course.thumbnailUrl ? <img src={course.thumbnailUrl} alt="Course" /> : <div className="no-image">No image</div>}
        </div>
        <div>
          <h3>Course image</h3>
          <p className="muted small">Shown on course cards and the course page. Best size: 750 x 422 pixels.</p>
          <label className="btn btn-outline">
            {imageUploading ? "Uploading..." : "Upload image"}
            <input type="file" accept="image/*" hidden onChange={handleImageChange} disabled={imageUploading} />
          </label>
        </div>
      </div>

      <div className="media-block">
        <div className="media-preview">
          {course.promoVideoUrl ? (
            <video src={course.promoVideoUrl} controls />
          ) : (
            <div className="no-image">No promo video</div>
          )}
        </div>
        <div>
          <h3>Promo video</h3>
          <p className="muted small">
            A short video that tells students what they will learn. The free Cloudinary plan accepts videos up to 100
            MB.
          </p>
          {videoProgress !== null ? (
            <div>
              <ProgressBar percent={videoProgress} />
              <p className="small">
                {videoProgress < 100 ? "Uploading " + videoProgress + "%" : "Processing video, please wait..."}
              </p>
            </div>
          ) : (
            <label className="btn btn-outline">
              Upload video
              <input type="file" accept="video/*" hidden onChange={handleVideoChange} />
            </label>
          )}
        </div>
      </div>
    </div>
  );
}

export default CourseMediaForm;
