import { useEffect, useState } from "react";
import api, { getErrorMessage } from "../api";
import { useAuth } from "../AuthContext";

// Lets an enrolled student rate a course (1 - 5 stars) and write a comment.
// If the student already left a review, the form is filled with it so they can change it.
function ReviewForm({ courseId }) {
  const { user } = useAuth();
  const [rating, setRating] = useState(0);
  const [hoverRating, setHoverRating] = useState(0);
  const [comment, setComment] = useState("");
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");

  useEffect(
    function () {
      api.get("/courses/" + courseId + "/reviews").then(function (response) {
        const myReview = response.data.find((r) => r.userId === user.id);
        if (myReview) {
          setRating(myReview.rating);
          setComment(myReview.comment || "");
        }
      });
    },
    [courseId, user.id]
  );

  async function handleSubmit(event) {
    event.preventDefault();
    setMessage("");
    setError("");

    if (rating === 0) {
      setError("Please choose a number of stars.");
      return;
    }

    try {
      const response = await api.post("/courses/" + courseId + "/reviews", { rating: rating, comment: comment });
      setMessage(response.data.message);
    } catch (err) {
      setError(getErrorMessage(err));
    }
  }

  const shownRating = hoverRating || rating;

  return (
    <form onSubmit={handleSubmit} className="review-form">
      <h3>How would you rate this course?</h3>

      <div className="star-picker" onMouseLeave={() => setHoverRating(0)}>
        {[1, 2, 3, 4, 5].map(function (star) {
          return (
            <button
              type="button"
              key={star}
              className={star <= shownRating ? "star filled" : "star"}
              onMouseEnter={() => setHoverRating(star)}
              onClick={() => setRating(star)}
            >
              ★
            </button>
          );
        })}
      </div>

      <textarea
        rows={4}
        placeholder="Tell other students what you think about this course (optional)"
        value={comment}
        onChange={(e) => setComment(e.target.value)}
      />

      {error && <div className="alert alert-error">{error}</div>}
      {message && <div className="alert alert-success">{message}</div>}

      <button type="submit" className="btn btn-primary">
        Save review
      </button>
    </form>
  );
}

export default ReviewForm;
