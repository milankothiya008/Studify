import { useState } from "react";
import { AlertCircle, ImagePlus, Video } from "lucide-react";
import api, { getErrorMessage } from "../../api";
import { useToast } from "../../ToastContext";
import ProgressBar from "../../components/ProgressBar";
import { getUploadPercent } from "../../utils";

// The "Image & promo video" tab.
function CourseMediaForm({ course, onChanged }) {
  const showToast = useToast();
  const [imageUploading, setImageUploading] = useState(false);
  const [videoProgress, setVideoProgress] = useState(null); // null = not uploading
  const [error, setError] = useState("");

  async function handleImageChange(event) {
    const file = event.target.files[0];
    event.target.value = ""; // so choosing the same file again works
    if (!file) {
      return;
    }

    const formData = new FormData();
    formData.append("file", file);

    setError("");
    setImageUploading(true);
    try {
      await api.post("/instructor/courses/" + course.id + "/thumbnail", formData);
      showToast("Course image uploaded.");
      onChanged();
    } catch (err) {
      setError(getErrorMessage(err));
    }
    setImageUploading(false);
  }

  async function handleVideoChange(event) {
    const file = event.target.files[0];
    event.target.value = "";
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
          setVideoProgress(getUploadPercent(progressEvent));
        },
      });
      showToast("Promo video uploaded.");
      onChanged();
    } catch (err) {
      setError(getErrorMessage(err));
    }
    setVideoProgress(null);
  }

  return (
    <div>
      <h2>Image &amp; promo video</h2>
      <p className="muted">A great image and a short promo video make students want to join.</p>

      {error && (
        <div className="alert alert-error">
          <AlertCircle size={18} /> {error}
        </div>
      )}

      <div className="media-block">
        <div className="media-preview">
          {course.thumbnailUrl ? (
            <img src={course.thumbnailUrl} alt="Course" />
          ) : (
            <div className="no-image">
              <ImagePlus size={36} />
            </div>
          )}
        </div>
        <div>
          <h3>Course image</h3>
          <p className="muted small">Shown on course cards and the course page. Best size: 750 × 422 pixels.</p>
          <label className={imageUploading ? "upload-zone disabled" : "upload-zone"}>
            {imageUploading ? <span className="btn-spinner btn-spinner-dark"></span> : <ImagePlus size={22} />}
            <span>{imageUploading ? "Uploading..." : course.thumbnailUrl ? "Replace image" : "Upload image"}</span>
            <small>JPG, PNG, WEBP or GIF</small>
            <input type="file" accept="image/*" hidden onChange={handleImageChange} disabled={imageUploading} />
          </label>
        </div>
      </div>

      <div className="media-block">
        <div className="media-preview">
          {course.promoVideoUrl ? (
            <video src={course.promoVideoUrl} controls preload="metadata" />
          ) : (
            <div className="no-image">
              <Video size={36} />
            </div>
          )}
        </div>
        <div>
          <h3>Promo video</h3>
          <p className="muted small">
            A short video that tells students what they will learn. The free Cloudinary plan accepts videos up to 100 MB.
          </p>
          {videoProgress !== null ? (
            <div className="upload-status">
              <ProgressBar percent={videoProgress} />
              <small>{videoProgress < 100 ? "Uploading " + videoProgress + "%" : "Processing video, please wait..."}</small>
            </div>
          ) : (
            <label className="upload-zone">
              <Video size={22} />
              <span>{course.promoVideoUrl ? "Replace video" : "Upload video"}</span>
              <small>MP4, MOV or WEBM</small>
              <input type="file" accept="video/*" hidden onChange={handleVideoChange} />
            </label>
          )}
        </div>
      </div>
    </div>
  );
}

export default CourseMediaForm;
