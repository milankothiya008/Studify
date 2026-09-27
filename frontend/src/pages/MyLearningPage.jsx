import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import api from "../api";
import ProgressBar from "../components/ProgressBar";
import Spinner from "../components/Spinner";

// All courses the student is enrolled in, with their progress.
function MyLearningPage() {
  const [courses, setCourses] = useState([]);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState("all");

  useEffect(function () {
    api
      .get("/enrollments/my")
      .then(function (response) {
        setCourses(response.data);
      })
      .finally(function () {
        setLoading(false);
      });
  }, []);

  const shownCourses = courses.filter(function (course) {
    if (filter === "in-progress") {
      return course.progressPercent < 100;
    }
    if (filter === "completed") {
      return course.progressPercent === 100;
    }
    return true;
  });

  return (
    <div>
      <section className="page-banner">
        <div className="container">
          <h1>My learning</h1>
          <div className="tabs tabs-light">
            <button className={filter === "all" ? "tab active" : "tab"} onClick={() => setFilter("all")}>
              All courses ({courses.length})
            </button>
            <button
              className={filter === "in-progress" ? "tab active" : "tab"}
              onClick={() => setFilter("in-progress")}
            >
              In progress
            </button>
            <button className={filter === "completed" ? "tab active" : "tab"} onClick={() => setFilter("completed")}>
              Completed
            </button>
          </div>
        </div>
      </section>

      <div className="container page">
        {loading && <Spinner />}

        {!loading && courses.length === 0 && (
          <div className="empty-state">
            <h2>You haven't enrolled in any course yet</h2>
            <p>When you enroll in a course, it will appear here.</p>
            <Link to="/courses" className="btn btn-primary">
              Browse courses
            </Link>
          </div>
        )}

        <div className="course-grid">
          {shownCourses.map(function (course) {
            let buttonText = "Start course";
            if (course.progressPercent === 100) {
              buttonText = "Watch again";
            } else if (course.completedLectures > 0 || course.lastLectureId) {
              buttonText = "Continue";
            }

            return (
              <div key={course.courseId} className="course-card my-course">
                <div className="course-card-image">
                  {course.thumbnailUrl ? <img src={course.thumbnailUrl} alt={course.title} /> : <div className="no-image">🎓</div>}
                  {!course.canWatch && <div className="locked-overlay">🔒 Subscription expired</div>}
                </div>
                <div className="course-card-body">
                  <h3>{course.title}</h3>
                  <p className="muted small">{course.instructorName}</p>

                  <ProgressBar percent={course.progressPercent} />
                  <p className="small">
                    {course.progressPercent}% complete · {course.completedLectures}/{course.totalLectures} lectures
                  </p>

                  {course.accessType === "Subscription" && <span className="badge">Included in subscription</span>}

                  <div className="card-actions">
                    {course.canWatch ? (
                      <Link to={"/learn/" + course.courseId} className="btn btn-primary btn-small">
                        {buttonText}
                      </Link>
                    ) : (
                      <Link to="/plans" className="btn btn-primary btn-small">
                        Renew subscription
                      </Link>
                    )}
                    {course.completedAt && (
                      <Link to={"/certificate/" + course.courseId} className="btn btn-outline btn-small">
                        🏆 Certificate
                      </Link>
                    )}
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}

export default MyLearningPage;
