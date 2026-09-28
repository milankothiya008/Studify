import { useNavigate } from "react-router-dom";
import { Heart } from "lucide-react";
import { useAuth } from "../AuthContext";
import { useWishlist } from "../WishlistContext";
import { useToast } from "../ToastContext";
import { getErrorMessage } from "../api";

// Heart button that saves a course to the wishlist.
// variant "icon" = round button on a course card, "button" = full button with text.
function WishlistButton({ courseId, variant }) {
  const { user } = useAuth();
  const { isSaved, toggle } = useWishlist();
  const showToast = useToast();
  const navigate = useNavigate();
  const saved = isSaved(courseId);

  async function handleClick(event) {
    // The heart sits inside a course card link: do not open the course.
    event.preventDefault();
    event.stopPropagation();

    if (!user) {
      navigate("/login", { state: { from: "/course/" + courseId } });
      return;
    }

    try {
      const nowSaved = await toggle(courseId);
      showToast(nowSaved ? "Added to your wishlist." : "Removed from your wishlist.", nowSaved ? "success" : "info");
    } catch (err) {
      showToast(getErrorMessage(err), "error");
    }
  }

  const label = saved ? "Remove from wishlist" : "Add to wishlist";

  if (variant === "button") {
    return (
      <button className={saved ? "btn btn-outline btn-block wishlist-saved" : "btn btn-outline btn-block"} onClick={handleClick}>
        <Heart size={18} className={saved ? "heart filled" : "heart"} /> {saved ? "In your wishlist" : "Add to wishlist"}
      </button>
    );
  }

  return (
    <button className={saved ? "heart-button saved" : "heart-button"} onClick={handleClick} title={label} aria-label={label}>
      <Heart size={18} />
    </button>
  );
}

export default WishlistButton;
