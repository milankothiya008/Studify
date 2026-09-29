import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { UserPlus } from "lucide-react";
import api, { getErrorMessage } from "../api";
import { useToast } from "../ToastContext";
import PageHeader from "../components/PageHeader";
import EmptyState from "../components/EmptyState";
import InstructorCard from "../components/InstructorCard";
import { SkeletonBlock } from "../components/Skeleton";

// Instructors the user follows. They get a notification when one of them publishes a new course.
function FollowingPage() {
  const showToast = useToast();
  const [instructors, setInstructors] = useState(null);

  useEffect(function () {
    api
      .get("/instructors/following")
      .then((response) => setInstructors(response.data))
      .catch(function (err) {
        showToast(getErrorMessage(err), "error");
        setInstructors([]);
      });
  }, []);

  // After unfollowing, the card stays (greyed out) so a wrong click can be undone.
  function handleFollowChange(instructorId, result) {
    setInstructors(
      instructors.map(function (instructor) {
        if (instructor.id !== instructorId) {
          return instructor;
        }
        return { ...instructor, isFollowing: result.isFollowing, followerCount: result.followerCount };
      })
    );
  }

  return (
    <div>
      <PageHeader title="Following" subtitle="Instructors you follow. We'll tell you when they publish a new course." />

      <div className="container page">
        {instructors === null ? (
          <div className="instructor-grid">
            {[0, 1, 2, 3].map((i) => (
              <div key={i} className="instructor-card instructor-card-skeleton">
                <SkeletonBlock height={84} width={84} round />
                <SkeletonBlock height={18} width="60%" />
                <SkeletonBlock height={12} width="80%" />
                <SkeletonBlock height={14} width="70%" />
              </div>
            ))}
          </div>
        ) : instructors.length === 0 ? (
          <EmptyState
            icon={UserPlus}
            title="You don't follow anyone yet"
            text="Open a course, click the instructor's name, and press Follow to hear about their new courses."
          >
            <Link to="/courses" className="btn btn-primary">
              Browse courses
            </Link>
          </EmptyState>
        ) : (
          <div className="instructor-grid">
            {instructors.map(function (instructor, index) {
              return (
                <div key={instructor.id} className={instructor.isFollowing ? "" : "instructor-unfollowed"}>
                  <InstructorCard instructor={instructor} index={index} onFollowChange={handleFollowChange} />
                </div>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
}

export default FollowingPage;
