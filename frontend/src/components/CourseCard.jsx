import { Link } from "react-router-dom";
import StarRating from "./StarRating";
import { formatDuration, formatPrice } from "../utils";

// One course in a grid of courses.
function CourseCard({ course }) {
  return (
    <Link to={"/course/" + course.id} className="course-card">
      <div className="course-card-image">
        {course.thumbnailUrl ? <img src={course.thumbnailUrl} alt={course.title} /> : <div className="no-image">🎓</div>}
      </div>
      <div className="course-card-body">
        <h3>{course.title}</h3>
        <p className="muted small">{course.instructorName}</p>
        <StarRating rating={course.averageRating} count={course.reviewCount} />
        <p className="muted small">
          {formatDuration(course.totalDurationSeconds)} total · {course.lectureCount} lectures · {course.level}
        </p>
        <p className="price">{formatPrice(course.price)}</p>
      </div>
    </Link>
  );
}

export default CourseCard;
