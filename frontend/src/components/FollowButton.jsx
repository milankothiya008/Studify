import { useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { UserCheck, UserMinus, UserPlus } from "lucide-react";
import api, { getErrorMessage } from "../api";
import { useAuth } from "../AuthContext";
import { useToast } from "../ToastContext";

// "Follow" / "Following" button for an instructor.
// onChange gets the answer of the server: { isFollowing, followerCount }.
// light = white button for dark backgrounds. small = smaller button for cards.
function FollowButton({ instructorId, isFollowing, onChange, light, small }) {
  const { user } = useAuth();
  const showToast = useToast();
  const navigate = useNavigate();
  const location = useLocation();
  const [working, setWorking] = useState(false);

  // Nobody can follow themselves.
  if (user && user.id === instructorId) {
    return null;
  }

  async function handleClick(event) {
    // The button can sit inside a clickable card.
    event.preventDefault();
    event.stopPropagation();

    if (!user) {
      navigate("/login", { state: { from: location.pathname } });
      return;
    }

    setWorking(true);
    try {
      const url = "/instructors/" + instructorId + "/follow";
      const response = isFollowing ? await api.delete(url) : await api.post(url);
      showToast(response.data.message);
      onChange(response.data);
    } catch (err) {
      showToast(getErrorMessage(err), "error");
    }
    setWorking(false);
  }

  let className = "btn follow-button";
  if (isFollowing) {
    className += light ? " btn-glass following" : " btn-outline following";
  } else {
    className += light ? " btn-white" : " btn-primary";
  }
  if (small) {
    className += " btn-small";
  }

  return (
    <button className={className} onClick={handleClick} disabled={working} aria-pressed={isFollowing}>
      {working ? (
        <span className="btn-spinner"></span>
      ) : isFollowing ? (
        <>
          {/* "Following" changes to "Unfollow" when the mouse is over the button. */}
          <span className="follow-state">
            <UserCheck size={17} /> Following
          </span>
          <span className="follow-hover">
            <UserMinus size={17} /> Unfollow
          </span>
        </>
      ) : (
        <>
          <UserPlus size={17} /> Follow
        </>
      )}
    </button>
  );
}

export default FollowButton;
