// Shows a rating like:  4.5 ★★★★☆ (12)
function StarRating({ rating, count }) {
  const rounded = Math.round(rating);
  let stars = "";
  for (let i = 1; i <= 5; i++) {
    stars = stars + (i <= rounded ? "★" : "☆");
  }

  if (count === 0) {
    return <span className="rating rating-empty">No ratings yet</span>;
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
