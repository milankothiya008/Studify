import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Heart } from "lucide-react";
import api from "../api";
import { useWishlist } from "../WishlistContext";
import PageHeader from "../components/PageHeader";
import CourseCard from "../components/CourseCard";
import EmptyState from "../components/EmptyState";
import { CourseGridSkeleton } from "../components/Skeleton";

// Courses the user saved with the heart button.
function WishlistPage() {
  const { savedIds } = useWishlist();
  const [courses, setCourses] = useState([]);
  const [loading, setLoading] = useState(true);

  // Load again whenever a heart is clicked (so removed courses disappear).
  useEffect(
    function () {
      api
        .get("/wishlist")
        .then(function (response) {
          setCourses(response.data);
        })
        .finally(function () {
          setLoading(false);
        });
    },
    [savedIds]
  );

  return (
    <div>
      <PageHeader title="Wishlist" subtitle="Courses you saved for later." />

      <div className="container page">
        {loading && <CourseGridSkeleton count={4} />}

        {!loading && courses.length === 0 && (
          <EmptyState icon={Heart} title="Your wishlist is empty" text="Tap the heart on any course to save it here.">
            <Link to="/courses" className="btn btn-primary">
              Browse courses
            </Link>
          </EmptyState>
        )}

        {!loading && courses.length > 0 && (
          <div className="course-grid">
            {courses.map(function (course, index) {
              return <CourseCard key={course.id} course={course} index={index} />;
            })}
          </div>
        )}
      </div>
    </div>
  );
}

export default WishlistPage;
