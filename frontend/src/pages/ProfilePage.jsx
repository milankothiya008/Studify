import { useState } from "react";
import { Link } from "react-router-dom";
import { AlertCircle, Camera, ExternalLink, KeyRound, UserRound } from "lucide-react";
import api, { getErrorMessage } from "../api";
import { useAuth } from "../AuthContext";
import { useToast } from "../ToastContext";
import Avatar from "../components/Avatar";
import PageHeader from "../components/PageHeader";
import PasswordInput from "../components/PasswordInput";

function ProfilePage() {
  const { user, setUser, saveToken } = useAuth();
  const showToast = useToast();

  const [fullName, setFullName] = useState(user.fullName);
  const [headline, setHeadline] = useState(user.headline || "");
  const [bio, setBio] = useState(user.bio || "");
  const [links, setLinks] = useState({
    websiteUrl: user.websiteUrl || "",
    linkedInUrl: user.linkedInUrl || "",
    youTubeUrl: user.youTubeUrl || "",
    twitterUrl: user.twitterUrl || "",
  });
  const [profileError, setProfileError] = useState("");
  const [savingProfile, setSavingProfile] = useState(false);
  const [uploading, setUploading] = useState(false);

  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [passwordError, setPasswordError] = useState("");
  const [savingPassword, setSavingPassword] = useState(false);

  async function handleSaveProfile(event) {
    event.preventDefault();
    setProfileError("");
    setSavingProfile(true);
    try {
      const response = await api.put("/account/profile", { fullName: fullName, headline: headline, bio: bio, ...links });
      setUser(response.data);
      // The server cleans the links ("example.com" -> "https://example.com/"), show the saved version.
      setLinks({
        websiteUrl: response.data.websiteUrl || "",
        linkedInUrl: response.data.linkedInUrl || "",
        youTubeUrl: response.data.youTubeUrl || "",
        twitterUrl: response.data.twitterUrl || "",
      });
      showToast("Profile saved.");
    } catch (err) {
      setProfileError(getErrorMessage(err));
    }
    setSavingProfile(false);
  }

  function setLink(name, value) {
    const copy = { ...links };
    copy[name] = value;
    setLinks(copy);
  }

  async function handlePhotoChange(event) {
    const file = event.target.files[0];
    event.target.value = ""; // so choosing the same file again works
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
      showToast("Photo updated.");
    } catch (err) {
      setProfileError(getErrorMessage(err));
    }
    setUploading(false);
  }

  async function handleChangePassword(event) {
    event.preventDefault();
    setPasswordError("");
    setSavingPassword(true);
    try {
      const response = await api.post("/account/change-password", {
        currentPassword: currentPassword,
        newPassword: newPassword,
      });
      // Other devices are logged out; this one gets a fresh login token.
      saveToken(response.data.token);
      showToast(response.data.message);
      setCurrentPassword("");
      setNewPassword("");
    } catch (err) {
      setPasswordError(getErrorMessage(err));
    }
    setSavingPassword(false);
  }

  return (
    <div>
      <PageHeader title="Account settings" subtitle="Manage your profile, photo and password." />

      <div className="container page narrow">
        {/* ---------- Profile ---------- */}
        <section className="card card-padded settings-card">
          <h2 className="card-title">
            <UserRound size={20} /> Profile
          </h2>

          <div className="photo-row">
            <label className="avatar-upload" title="Change photo">
              <Avatar name={user.fullName} imageUrl={user.profileImageUrl} size={96} />
              <span className="avatar-upload-overlay">
                {uploading ? <span className="btn-spinner"></span> : <Camera size={22} />}
              </span>
              <input type="file" accept="image/*" hidden onChange={handlePhotoChange} disabled={uploading} />
            </label>
            <div>
              <strong>{user.fullName}</strong>
              <p className="muted small">
                {user.email} · {user.role}
              </p>
              <p className="muted small">Click the photo to change it (JPG, PNG or WEBP).</p>
              {user.role === "Instructor" && (
                <Link to={"/instructors/" + user.id} className="text-link small">
                  View my public profile <ExternalLink size={14} />
                </Link>
              )}
            </div>
          </div>

          {profileError && (
            <div className="alert alert-error">
              <AlertCircle size={18} /> {profileError}
            </div>
          )}

          <form onSubmit={handleSaveProfile}>
            <div className="form-field">
              <label htmlFor="fullName">Full name</label>
              <input id="fullName" value={fullName} onChange={(e) => setFullName(e.target.value)} maxLength={100} required />
            </div>

            <div className="form-field">
              <label htmlFor="headline">Headline</label>
              <input
                id="headline"
                value={headline}
                placeholder="e.g. Web developer and teacher"
                maxLength={150}
                onChange={(e) => setHeadline(e.target.value)}
              />
            </div>

            <div className="form-field">
              <label htmlFor="bio">Biography</label>
              <textarea id="bio" rows={5} value={bio} onChange={(e) => setBio(e.target.value)} />
              {user.role !== "Student" && (
                <p className="field-hint">Your headline and biography are shown on your course pages and public profile.</p>
              )}
            </div>

            {user.role !== "Student" && (
              <fieldset className="links-fieldset">
                <legend>Links on your public profile (optional)</legend>
                <div className="form-row">
                  <div className="form-field">
                    <label htmlFor="websiteUrl">Website</label>
                    <input
                      id="websiteUrl"
                      value={links.websiteUrl}
                      placeholder="yourwebsite.com"
                      maxLength={300}
                      onChange={(e) => setLink("websiteUrl", e.target.value)}
                    />
                  </div>
                  <div className="form-field">
                    <label htmlFor="linkedInUrl">LinkedIn</label>
                    <input
                      id="linkedInUrl"
                      value={links.linkedInUrl}
                      placeholder="linkedin.com/in/your-name"
                      maxLength={300}
                      onChange={(e) => setLink("linkedInUrl", e.target.value)}
                    />
                  </div>
                  <div className="form-field">
                    <label htmlFor="youTubeUrl">YouTube</label>
                    <input
                      id="youTubeUrl"
                      value={links.youTubeUrl}
                      placeholder="youtube.com/@your-channel"
                      maxLength={300}
                      onChange={(e) => setLink("youTubeUrl", e.target.value)}
                    />
                  </div>
                  <div className="form-field">
                    <label htmlFor="twitterUrl">X (Twitter)</label>
                    <input
                      id="twitterUrl"
                      value={links.twitterUrl}
                      placeholder="x.com/your-name"
                      maxLength={300}
                      onChange={(e) => setLink("twitterUrl", e.target.value)}
                    />
                  </div>
                </div>
              </fieldset>
            )}

            <button type="submit" className="btn btn-primary" disabled={savingProfile}>
              {savingProfile && <span className="btn-spinner"></span>}
              {savingProfile ? "Saving..." : "Save profile"}
            </button>
          </form>
        </section>

        {/* ---------- Password ---------- */}
        <section className="card card-padded settings-card">
          <h2 className="card-title">
            <KeyRound size={20} /> Change password
          </h2>
          <p className="muted small">Changing your password logs you out on all your other devices.</p>

          {passwordError && (
            <div className="alert alert-error">
              <AlertCircle size={18} /> {passwordError}
            </div>
          )}

          <form onSubmit={handleChangePassword}>
            <div className="form-field">
              <label>Current password</label>
              <PasswordInput
                value={currentPassword}
                onChange={(e) => setCurrentPassword(e.target.value)}
                autoComplete="current-password"
              />
            </div>

            <div className="form-field">
              <label>New password</label>
              <PasswordInput
                value={newPassword}
                minLength={8}
                onChange={(e) => setNewPassword(e.target.value)}
                autoComplete="new-password"
              />
            </div>

            <button type="submit" className="btn btn-dark" disabled={savingPassword}>
              {savingPassword && <span className="btn-spinner"></span>}
              {savingPassword ? "Saving..." : "Change password"}
            </button>
          </form>
        </section>
      </div>
    </div>
  );
}

export default ProfilePage;
