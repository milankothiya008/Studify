import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import api, { getErrorMessage } from "../../api";
import Spinner from "../../components/Spinner";
import { formatDate, formatMoney, formatPrice } from "../../utils";

function InstructorDashboardPage() {
  const navigate = useNavigate();
  const [dashboard, setDashboard] = useState(null);
  const [newTitle, setNewTitle] = useState("");
  const [error, setError] = useState("");
  const [creating, setCreating] = useState(false);

  useEffect(function () {
    api
      .get("/instructor/dashboard")
      .then(function (response) {
        setDashboard(response.data);
      })
      .catch(function (err) {
        setError(getErrorMessage(err));
      });
  }, []);

  // Creates a draft course with just a title, then opens the editor.
  async function handleCreateCourse(event) {
    event.preventDefault();
    setError("");
    setCreating(true);
    try {
      const response = await api.post("/instructor/courses", {
        title: newTitle,
        price: 0,
        level: "All Levels",
        language: "English",
      });
      navigate("/instructor/course/" + response.data.id);
    } catch (err) {
      setError(getErrorMessage(err));
      setCreating(false);
    }
  }

  if (!dashboard && !error) {
    return <Spinner />;
  }

  return (
    <div className="container page">
      <h1>Instructor dashboard</h1>

      {error && <div className="alert alert-error">{error}</div>}

      {dashboard && (
        <>
          <div className="stat-grid">
            <div className="stat">
              <span>Total revenue</span>
              <strong>{formatMoney(dashboard.totalRevenue)}</strong>
            </div>
            <div className="stat">
              <span>Total students</span>
              <strong>{dashboard.totalStudents}</strong>
            </div>
            <div className="stat">
              <span>Instructor rating</span>
              <strong>{dashboard.averageRating > 0 ? dashboard.averageRating.toFixed(1) + " ★" : "-"}</strong>
            </div>
            <div className="stat">
              <span>Courses</span>
              <strong>{dashboard.totalCourses}</strong>
            </div>
          </div>

          <form className="box create-course" onSubmit={handleCreateCourse}>
            <h3>Create a new course</h3>
            <p className="muted small">Start with a working title. You can change it later.</p>
            <div className="inline-form">
              <input
                placeholder="e.g. Learn Photoshop CS6 from Scratch"
                value={newTitle}
                minLength={3}
                onChange={(e) => setNewTitle(e.target.value)}
                required
              />
              <button type="submit" className="btn btn-primary" disabled={creating}>
                {creating ? "Creating..." : "Create course"}
              </button>
            </div>
          </form>

          <h2>Your courses</h2>
          {dashboard.courses.length === 0 && <p className="muted">You have no courses yet. Create your first one above!</p>}

          <div className="instructor-course-list">
            {dashboard.courses.map(function (course) {
              return (
                <div key={course.id} className="instructor-course">
                  <div className="instructor-course-image">
                    {course.thumbnailUrl ? <img src={course.thumbnailUrl} alt="" /> : <div className="no-image">🎓</div>}
                  </div>
                  <div className="instructor-course-info">
                    <h3>{course.title}</h3>
                    <p>
                      {course.isPublished ? (
                        <span className="badge badge-success">Published</span>
                      ) : (
                        <span className="badge badge-warning">Draft</span>
                      )}{" "}
                      <span className="muted small">
                        {formatPrice(course.price)} · {course.lectureCount} lectures · updated {formatDate(course.updatedAt)}
                      </span>
                    </p>
                  </div>
                  <div className="instructor-course-numbers">
                    <div>
                      <strong>{course.studentCount}</strong>
                      <small>students</small>
                    </div>
                    <div>
                      <strong>{course.reviewCount > 0 ? course.averageRating.toFixed(1) : "-"}</strong>
                      <small>rating</small>
                    </div>
                    <div>
                      <strong>{formatMoney(course.revenue)}</strong>
                      <small>revenue</small>
                    </div>
                  </div>
                  <div className="instructor-course-actions">
                    <Link to={"/instructor/course/" + course.id} className="btn btn-primary btn-small">
                      Edit
                    </Link>
                    <Link to={"/course/" + course.id} className="btn btn-outline btn-small">
                      View
                    </Link>
                  </div>
                </div>
              );
            })}
          </div>
        </>
      )}
    </div>
  );
}

export default InstructorDashboardPage;
