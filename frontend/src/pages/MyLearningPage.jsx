import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Award, BookOpen, CheckCircle2, Lock, PlayCircle } from "lucide-react";
import api from "../api";
import PageHeader from "../components/PageHeader";
import ProgressBar from "../components/ProgressBar";
import EmptyState from "../components/EmptyState";
import { CourseGridSkeleton } from "../components/Skeleton";

// All courses the student is enrolled in, with their progress.
function MyLearningPage() {
  const [courses, setCourses] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);
  const [filter, setFilter] = useState("all");

  useEffect(function () {
    api
      .get("/enrollments/my")
      .then(function (response) {
        setCourses(response.data);
      })
      .catch(function () {
        setError(true);
      })
      .finally(function () {
        setLoading(false);
      });
  }, []);

  const inProgressCount = courses.filter((c) => c.progressPercent < 100).length;
  const completedCount = courses.length - inProgressCount;

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
      <PageHeader title="My learning" subtitle="Pick up right where you left off." />

      <div className="container page">
        <div className="tabs">
          <button className={filter === "all" ? "tab active" : "tab"} onClick={() => setFilter("all")}>
            All courses <span className="tab-count">{courses.length}</span>
          </button>
          <button className={filter === "in-progress" ? "tab active" : "tab"} onClick={() => setFilter("in-progress")}>
            In progress <span className="tab-count">{inProgressCount}</span>
          </button>
          <button className={filter === "completed" ? "tab active" : "tab"} onClick={() => setFilter("completed")}>
            Completed <span className="tab-count">{completedCount}</span>
          </button>
        </div>

        <div className="tab-content">
          {loading && <CourseGridSkeleton count={4} />}

          {!loading && error && <EmptyState icon={BookOpen} title="Could not load your courses" text="Please try again." />}

          {!loading && !error && courses.length === 0 && (
            <EmptyState
              icon={BookOpen}
              title="You haven't enrolled in any course yet"
              text="When you enroll in a course, it will appear here."
            >
              <Link to="/courses" className="btn btn-primary">
                Browse courses
              </Link>
            </EmptyState>
          )}

          {!loading && courses.length > 0 && shownCourses.length === 0 && (
            <EmptyState icon={CheckCircle2} title="Nothing here yet" text="No courses match this tab." />
          )}

          <div className="course-grid">
            {shownCourses.map(function (course, index) {
              let buttonText = "Start course";
              if (course.progressPercent === 100) {
                buttonText = "Watch again";
              } else if (course.completedLectures > 0 || course.lastLectureId) {
                buttonText = "Continue";
              }

              return (
                <div key={course.courseId} className="course-card my-course stagger-item" style={{ "--i": index }}>
                  <div className="course-card-image">
                    {course.thumbnailUrl ? (
                      <img src={course.thumbnailUrl} alt={course.title} loading="lazy" />
                    ) : (
                      <div className="no-image">
                        <PlayCircle size={40} />
                      </div>
                    )}
                    {!course.canWatch && (
                      <div className="locked-overlay">
                        <Lock size={22} /> Subscription expired
                      </div>
                    )}
                    {course.progressPercent === 100 && (
                      <span className="card-tag card-tag-success">
                        <CheckCircle2 size={14} /> Completed
                      </span>
                    )}
                  </div>
                  <div className="course-card-body">
                    <h3>{course.title}</h3>
                    <p className="course-card-instructor">{course.instructorName}</p>

                    <ProgressBar percent={course.progressPercent} />
                    <p className="progress-text">
                      <strong>{course.progressPercent}%</strong> complete · {course.completedLectures}/
                      {course.totalLectures} lectures
                    </p>

                    {course.accessType === "Subscription" && <span className="badge badge-brand">Included in plan</span>}

                    <div className="card-actions">
                      {course.canWatch ? (
                        <Link to={"/learn/" + course.courseId} className="btn btn-primary btn-small">
                          <PlayCircle size={16} /> {buttonText}
                        </Link>
                      ) : (
                        <Link to="/plans" className="btn btn-primary btn-small">
                          Renew subscription
                        </Link>
                      )}
                      {course.completedAt && (
                        <Link to={"/certificate/" + course.courseId} className="btn btn-outline btn-small">
                          <Award size={16} /> Certificate
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
    </div>
  );
}

export default MyLearningPage;
