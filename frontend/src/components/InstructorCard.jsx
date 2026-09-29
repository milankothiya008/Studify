import { Link } from "react-router-dom";
import { BookOpen, Star, Users } from "lucide-react";
import Avatar from "./Avatar";
import FollowButton from "./FollowButton";
import { formatCount } from "../utils";

// One instructor in a grid: photo, name, headline, numbers and a follow button.
// onFollowChange(instructorId, result) is called after follow / unfollow.
function InstructorCard({ instructor, index, onFollowChange }) {
  return (
    <Link to={"/instructors/" + instructor.id} className="instructor-card stagger-item" style={{ "--i": index || 0 }}>
      <div className="instructor-card-top">
        <span className="avatar-ring">
          <Avatar name={instructor.fullName} imageUrl={instructor.profileImageUrl} size={84} />
        </span>
      </div>
      <h3>{instructor.fullName}</h3>
      <p className="instructor-card-headline">{instructor.headline || "Instructor"}</p>

      <div className="instructor-card-stats">
        <span title="Rating">
          <Star size={14} className="star-icon filled" />
          {instructor.reviewCount > 0 ? instructor.averageRating.toFixed(1) : "New"}
        </span>
        <span title="Students">
          <Users size={14} /> {formatCount(instructor.studentCount)}
        </span>
        <span title="Courses">
          <BookOpen size={14} /> {instructor.courseCount}
        </span>
      </div>

      <div className="instructor-card-footer">
        <span className="muted small">
          {formatCount(instructor.followerCount)} {instructor.followerCount === 1 ? "follower" : "followers"}
        </span>
        {onFollowChange && (
          <FollowButton
            instructorId={instructor.id}
            isFollowing={instructor.isFollowing}
            onChange={(result) => onFollowChange(instructor.id, result)}
            small
          />
        )}
      </div>
    </Link>
  );
}

export default InstructorCard;
