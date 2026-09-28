import { Star } from "lucide-react";

// Shows a rating like:  4.5 ★★★★☆ (12)
function StarRating({ rating, count, size }) {
  if (count === 0) {
    return <span className="rating rating-empty">No ratings yet</span>;
  }

  const rounded = Math.round(rating);
  const stars = [];
  for (let i = 1; i <= 5; i++) {
    stars.push(<Star key={i} size={size || 14} className={i <= rounded ? "star-icon filled" : "star-icon"} />);
  }

  return (
    <span className="rating">
      <strong>{Number(rating).toFixed(1)}</strong>
      <span className="stars">{stars}</span>
      {count !== undefined && <span className="rating-count">({count})</span>}
    </span>
  );
}

export default StarRating;
