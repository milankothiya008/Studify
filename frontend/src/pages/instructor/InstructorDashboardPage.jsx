import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { AlertCircle, BookOpen, Eye, IndianRupee, Pencil, PlayCircle, Plus, Star, UserRound, Users, X } from "lucide-react";
import api, { getErrorMessage } from "../../api";
import { useAuth } from "../../AuthContext";
import PageHeader from "../../components/PageHeader";
import EmptyState from "../../components/EmptyState";
import { SkeletonRows } from "../../components/Skeleton";
import AnalyticsPanel from "./AnalyticsPanel";
import { formatDate, formatMoney, formatPrice } from "../../utils";

function InstructorDashboardPage() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const [dashboard, setDashboard] = useState(null);
  const [error, setError] = useState("");
  const [showCreate, setShowCreate] = useState(false);
  const [newTitle, setNewTitle] = useState("");
  const [createError, setCreateError] = useState("");
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
    setCreateError("");
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
      setCreateError(getErrorMessage(err));
      setCreating(false);
    }
  }

  const stats = dashboard
    ? [
        { icon: IndianRupee, label: "Total revenue", value: formatMoney(dashboard.totalRevenue) },
        { icon: Users, label: "Total students", value: dashboard.totalStudents },
        {
          icon: Star,
          label: "Average rating",
          value: dashboard.averageRating > 0 ? dashboard.averageRating.toFixed(1) : "-",
        },
        { icon: BookOpen, label: "Courses", value: dashboard.totalCourses },
      ]
    : [];

  return (
    <div>
      <PageHeader title="Instructor dashboard" subtitle="Create courses, upload lectures and follow your students.">
        {/* Admins without published courses have no public profile. */}
        {user.role === "Instructor" && (
          <Link to={"/instructors/" + user.id} className="btn btn-glass">
            <UserRound size={18} /> Public profile
          </Link>
        )}
        <button className="btn btn-white" onClick={() => setShowCreate(true)}>
          <Plus size={18} /> New course
        </button>
      </PageHeader>

      <div className="container page">
        {error && (
          <div className="alert alert-error">
            <AlertCircle size={18} /> {error}
          </div>
        )}

        {!dashboard && !error && <SkeletonRows rows={6} />}

        {dashboard && (
          <>
            <div className="stat-grid">
              {stats.map(function (stat, index) {
                const Icon = stat.icon;
                return (
                  <div key={stat.label} className="stat card stagger-item" style={{ "--i": index }}>
                    <span className="stat-icon">
                      <Icon size={22} />
                    </span>
                    <div>
                      <span className="stat-label">{stat.label}</span>
                      <strong className="stat-value">{stat.value}</strong>
                    </div>
                  </div>
                );
              })}
            </div>

            {/* Charts: new students and revenue per day */}
            <AnalyticsPanel />

            <div className="section-heading section-heading-spaced">
              <div>
                <h2>Your courses</h2>
              </div>
            </div>

            {dashboard.courses.length === 0 && (
              <EmptyState icon={BookOpen} title="You have no courses yet" text="Create your first course in a few minutes.">
                <button className="btn btn-primary" onClick={() => setShowCreate(true)}>
                  <Plus size={18} /> Create a course
                </button>
              </EmptyState>
            )}

            <div className="instructor-course-list">
              {dashboard.courses.map(function (course, index) {
                return (
                  <div key={course.id} className="instructor-course card stagger-item" style={{ "--i": index }}>
                    <div className="instructor-course-image">
                      {course.thumbnailUrl ? (
                        <img src={course.thumbnailUrl} alt="" />
                      ) : (
                        <div className="no-image">
                          <PlayCircle size={28} />
                        </div>
                      )}
                    </div>
                    <div className="instructor-course-info">
                      <h3>{course.title}</h3>
                      <p>
                        {course.isPublished ? (
                          <span className="badge badge-success">Published</span>
                        ) : (
                          <span className="badge badge-warning">Draft</span>
                        )}
                        <span className="muted small">
                          {formatPrice(course.price)} · {course.lectureCount} lectures · updated{" "}
                          {formatDate(course.updatedAt)}
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
                        <Pencil size={15} /> Edit
                      </Link>
                      <Link to={"/course/" + course.id} className="btn btn-outline btn-small">
                        <Eye size={15} /> View
                      </Link>
                    </div>
                  </div>
                );
              })}
            </div>
          </>
        )}
      </div>

      {/* ---------- "New course" popup ---------- */}
      {showCreate && (
        <div className="modal-backdrop" onClick={() => setShowCreate(false)}>
          <form className="modal card-padded" onClick={(e) => e.stopPropagation()} onSubmit={handleCreateCourse}>
            <div className="modal-header modal-header-plain">
              <h2>Create a new course</h2>
              <button type="button" className="icon-button" onClick={() => setShowCreate(false)} aria-label="Close">
                <X size={20} />
              </button>
            </div>
            <p className="muted">Start with a working title. You can change everything later.</p>

            {createError && (
              <div className="alert alert-error">
                <AlertCircle size={18} /> {createError}
              </div>
            )}

            <div className="form-field">
              <label htmlFor="newTitle">Course title</label>
              <input
                id="newTitle"
                placeholder="e.g. Learn Photoshop from Scratch"
                value={newTitle}
                minLength={3}
                maxLength={200}
                autoFocus
                onChange={(e) => setNewTitle(e.target.value)}
                required
              />
            </div>

            <div className="modal-actions">
              <button type="button" className="btn btn-ghost" onClick={() => setShowCreate(false)}>
                Cancel
              </button>
              <button type="submit" className="btn btn-primary" disabled={creating}>
                {creating && <span className="btn-spinner"></span>}
                {creating ? "Creating..." : "Create course"}
              </button>
            </div>
          </form>
        </div>
      )}
    </div>
  );
}

export default InstructorDashboardPage;
