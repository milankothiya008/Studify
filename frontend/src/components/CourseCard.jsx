import { Link } from "react-router-dom";
import { BarChart3, Clock, PlayCircle } from "lucide-react";
import StarRating from "./StarRating";
import { formatDuration, formatPrice } from "../utils";

// One course in a grid of courses.
// index is its position in the list; it makes the cards appear one after another.
function CourseCard({ course, index }) {
  return (
    <Link to={"/course/" + course.id} className="course-card stagger-item" style={{ "--i": index || 0 }}>
      <div className="course-card-image">
        {course.thumbnailUrl ? (
          <img src={course.thumbnailUrl} alt={course.title} loading="lazy" />
        ) : (
          <div className="no-image">
            <PlayCircle size={40} />
          </div>
        )}
        {course.categoryName && <span className="card-tag">{course.categoryName}</span>}
      </div>

      <div className="course-card-body">
        <h3>{course.title}</h3>
        <p className="course-card-instructor">{course.instructorName}</p>
        <StarRating rating={course.averageRating} count={course.reviewCount} />
        <div className="course-card-meta">
          <span>
            <Clock size={14} /> {formatDuration(course.totalDurationSeconds)}
          </span>
          <span>
            <PlayCircle size={14} /> {course.lectureCount} lectures
          </span>
          <span>
            <BarChart3 size={14} /> {course.level}
          </span>
        </div>
        <div className="course-card-footer">
          <span className={Number(course.price) === 0 ? "price price-free" : "price"}>{formatPrice(course.price)}</span>
        </div>
      </div>
    </Link>
  );
}

export default CourseCard;
