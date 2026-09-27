import { useState } from "react";
import api, { getErrorMessage } from "../api";
import { useAuth } from "../AuthContext";
import Avatar from "../components/Avatar";

function ProfilePage() {
  const { user, setUser } = useAuth();

  const [fullName, setFullName] = useState(user.fullName);
  const [headline, setHeadline] = useState(user.headline || "");
  const [bio, setBio] = useState(user.bio || "");
  const [profileMessage, setProfileMessage] = useState("");
  const [profileError, setProfileError] = useState("");
  const [uploading, setUploading] = useState(false);

  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [passwordMessage, setPasswordMessage] = useState("");
  const [passwordError, setPasswordError] = useState("");

  async function handleSaveProfile(event) {
    event.preventDefault();
    setProfileMessage("");
    setProfileError("");
    try {
      const response = await api.put("/account/profile", { fullName: fullName, headline: headline, bio: bio });
      setUser(response.data);
      setProfileMessage("Profile saved.");
    } catch (err) {
      setProfileError(getErrorMessage(err));
    }
  }

  async function handlePhotoChange(event) {
    const file = event.target.files[0];
    if (!file) {
      return;
    }

    // Files are sent as "multipart/form-data", not JSON.
    const formData = new FormData();
    formData.append("file", file);

    setUploading(true);
    setProfileError("");
    try {
      const response = await api.post("/account/photo", formData);
      setUser(response.data);
    } catch (err) {
      setProfileError(getErrorMessage(err));
    }
    setUploading(false);
  }

  async function handleChangePassword(event) {
    event.preventDefault();
    setPasswordMessage("");
    setPasswordError("");
    try {
      const response = await api.post("/account/change-password", {
        currentPassword: currentPassword,
        newPassword: newPassword,
      });
      setPasswordMessage(response.data.message);
      setCurrentPassword("");
      setNewPassword("");
    } catch (err) {
      setPasswordError(getErrorMessage(err));
    }
  }

  return (
    <div className="container page narrow">
      <h1>Profile</h1>

      <div className="box">
        <div className="photo-row">
          <Avatar name={user.fullName} imageUrl={user.profileImageUrl} size={96} />
          <div>
            <label className="btn btn-outline">
              {uploading ? "Uploading..." : "Change photo"}
              <input type="file" accept="image/*" hidden onChange={handlePhotoChange} />
            </label>
            <p className="muted small">JPG, PNG or WEBP.</p>
          </div>
        </div>

        <form onSubmit={handleSaveProfile}>
          <label>Full name</label>
          <input value={fullName} onChange={(e) => setFullName(e.target.value)} required />

          <label>Headline</label>
          <input
            value={headline}
            placeholder="e.g. Web developer and teacher"
            onChange={(e) => setHeadline(e.target.value)}
          />

          <label>Biography</label>
          <textarea rows={5} value={bio} onChange={(e) => setBio(e.target.value)} />
          {user.role === "Instructor" && (
            <p className="muted small">Your headline and biography are shown on your course pages.</p>
          )}

          {profileError && <div className="alert alert-error">{profileError}</div>}
          {profileMessage && <div className="alert alert-success">{profileMessage}</div>}

          <button type="submit" className="btn btn-primary">
            Save
          </button>
        </form>
      </div>

      <div className="box">
        <h2>Change password</h2>
        <form onSubmit={handleChangePassword}>
          <label>Current password</label>
          <input
            type="password"
            value={currentPassword}
            onChange={(e) => setCurrentPassword(e.target.value)}
            required
          />

          <label>New password</label>
          <input
            type="password"
            value={newPassword}
            minLength={6}
            onChange={(e) => setNewPassword(e.target.value)}
            required
          />

          {passwordError && <div className="alert alert-error">{passwordError}</div>}
          {passwordMessage && <div className="alert alert-success">{passwordMessage}</div>}

          <button type="submit" className="btn btn-dark">
            Change password
          </button>
        </form>
      </div>
    </div>
  );
}

export default ProfilePage;
