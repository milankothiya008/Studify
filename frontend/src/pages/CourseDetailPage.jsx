import { useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import api, { getErrorMessage } from "../api";
import { useAuth } from "../AuthContext";
import StarRating from "../components/StarRating";
import Avatar from "../components/Avatar";
import Spinner from "../components/Spinner";
import { formatClock, formatDate, formatDuration, formatPrice, splitLines } from "../utils";

// The course landing page (like a Udemy course page).
function CourseDetailPage() {
  const { id } = useParams();
  const { user } = useAuth();
  const navigate = useNavigate();

  const [course, setCourse] = useState(null);
  const [reviews, setReviews] = useState([]);
  const [error, setError] = useState("");
  const [actionError, setActionError] = useState("");
  const [working, setWorking] = useState(false);
  const [openSections, setOpenSections] = useState({}); // which sections are expanded
  const [previewLecture, setPreviewLecture] = useState(null); // lecture shown in the popup

  useEffect(
    function () {
      async function loadCourse() {
        try {
          const courseResponse = await api.get("/courses/" + id);
          const reviewsResponse = await api.get("/courses/" + id + "/reviews");
          setCourse(courseResponse.data);
          setReviews(reviewsResponse.data);

          // Open the first section by default.
          if (courseResponse.data.sections.length > 0) {
            const open = {};
            open[courseResponse.data.sections[0].id] = true;
            setOpenSections(open);
          }
        } catch (err) {
          setError(getErrorMessage(err));
        }
      }
      loadCourse();
    },
    [id, user]
  );

  function toggleSection(sectionId) {
    const copy = { ...openSections };
    copy[sectionId] = !copy[sectionId];
    setOpenSections(copy);
  }

  function expandAll() {
    const all = {};
    course.sections.forEach(function (section) {
      all[section.id] = true;
    });
    setOpenSections(all);
  }

  // Free course, or paid course with an active subscription.
  async function handleEnroll() {
    if (!user) {
      navigate("/login", { state: { from: "/course/" + id } });
      return;
    }

    setWorking(true);
    setActionError("");
    try {
      await api.post("/enrollments/" + id);
      navigate("/learn/" + id);
    } catch (err) {
      setActionError(getErrorMessage(err));
    }
    setWorking(false);
  }

  function handleBuy() {
    if (!user) {
      navigate("/login", { state: { from: "/checkout/course/" + id } });
      return;
    }
    navigate("/checkout/course/" + id);
  }

  if (error) {
    return (
      <div className="container page">
        <div className="empty-state">
          <h2>{error}</h2>
          <Link to="/courses" className="btn btn-primary">
            Browse courses
          </Link>
        </div>
      </div>
    );
  }

  if (!course) {
    return <Spinner />;
  }

  const isFree = Number(course.price) === 0;
  const learnItems = splitLines(course.whatYouWillLearn);
  const requirementItems = splitLines(course.requirements);
  // Enrolled with a subscription that has now expired:
  const subscriptionExpired = course.isEnrolled && !course.canWatch;

  return (
    <div>
      {/* Dark header */}
      <section className="course-header">
        <div className="container course-header-inner">
          <div className="course-header-text">
            {course.categoryName && (
              <Link to={"/courses?categoryId=" + course.categoryId} className="breadcrumb">
                {course.categoryName}
              </Link>
            )}
            <h1>{course.title}</h1>
            <p className="subtitle">{course.subtitle}</p>
            <div className="course-meta">
              <StarRating rating={course.averageRating} count={course.reviewCount} />
              <span>
                {course.studentCount} {course.studentCount === 1 ? "student" : "students"}
              </span>
            </div>
            <p>
              Created by <strong>{course.instructorName}</strong>
            </p>
            <p className="small">
              Last updated {formatDate(course.updatedAt)} · {course.language} · {course.level}
            </p>
            {!course.isPublished && <span className="badge badge-warning">Draft - only you can see this page</span>}
          </div>
        </div>
      </section>

      <div className="container course-body">
        <div className="course-main">
          {/* What you'll learn */}
          {learnItems.length > 0 && (
            <div className="box">
              <h2>What you'll learn</h2>
              <ul className="check-list two-columns">
                {learnItems.map(function (item, index) {
                  return <li key={index}>{item}</li>;
                })}
              </ul>
            </div>
          )}

          {/* Curriculum */}
          <h2>Course content</h2>
          <div className="curriculum-summary">
            <span>
              {course.sections.length} sections · {course.lectureCount} lectures ·{" "}
              {formatDuration(course.totalDurationSeconds)} total length
            </span>
            <button className="link-button" onClick={expandAll}>
              Expand all sections
            </button>
          </div>

          <div className="accordion">
            {course.sections.map(function (section) {
              const sectionSeconds = section.lectures.reduce((sum, l) => sum + l.durationSeconds, 0);
              return (
                <div key={section.id} className="accordion-item">
                  <button className="accordion-header" onClick={() => toggleSection(section.id)}>
                    <span>
                      {openSections[section.id] ? "▾" : "▸"} {section.title}
                    </span>
                    <span className="muted small">
                      {section.lectures.length} lectures · {formatDuration(sectionSeconds)}
                    </span>
                  </button>

                  {openSections[section.id] && (
                    <ul className="lecture-list">
                      {section.lectures.map(function (lecture) {
                        return (
                          <li key={lecture.id}>
                            <span>▶ {lecture.title}</span>
                            <span className="lecture-right">
                              {lecture.isFreePreview && lecture.videoUrl && (
                                <button className="link-button" onClick={() => setPreviewLecture(lecture)}>
                                  Preview
                                </button>
                              )}
                              <span className="muted">{formatClock(lecture.durationSeconds)}</span>
                            </span>
                          </li>
                        );
                      })}
                    </ul>
                  )}
                </div>
              );
            })}
          </div>

          {/* Requirements */}
          {requirementItems.length > 0 && (
            <>
              <h2>Requirements</h2>
              <ul className="bullet-list">
                {requirementItems.map(function (item, index) {
                  return <li key={index}>{item}</li>;
                })}
              </ul>
            </>
          )}

          {/* Description */}
          <h2>Description</h2>
          <p className="pre-line">{course.description}</p>

          {/* Instructor */}
          <h2>Instructor</h2>
          <div className="instructor-block">
            <Avatar name={course.instructorName} imageUrl={course.instructorImageUrl} size={96} />
            <div>
              <h3>{course.instructorName}</h3>
              {course.instructorHeadline && <p className="muted">{course.instructorHeadline}</p>}
              {course.instructorBio && <p className="pre-line">{course.instructorBio}</p>}
            </div>
          </div>

          {/* Reviews */}
          <h2>Student reviews</h2>
          {reviews.length === 0 && <p className="muted">No reviews yet.</p>}
          <div className="review-list">
            {reviews.map(function (review) {
              return (
                <div key={review.id} className="review">
                  <Avatar name={review.userName} imageUrl={review.userImageUrl} size={40} />
                  <div>
                    <strong>{review.userName}</strong>
                    <div>
                      <StarRating rating={review.rating} />{" "}
                      <span className="muted small">{formatDate(review.createdAt)}</span>
                    </div>
                    {review.comment && <p>{review.comment}</p>}
                  </div>
                </div>
              );
            })}
          </div>
        </div>

        {/* Buy / enroll card on the right */}
        <aside className="buy-card">
          <div className="buy-card-media">
            {course.promoVideoUrl ? (
              <video src={course.promoVideoUrl} poster={course.thumbnailUrl} controls />
            ) : (
              course.thumbnailUrl && <img src={course.thumbnailUrl} alt={course.title} />
            )}
          </div>

          <div className="buy-card-body">
            {!course.canWatch && <div className="buy-price">{formatPrice(course.price)}</div>}

            {actionError && <div className="alert alert-error">{actionError}</div>}

            {/* 1. The instructor of this course */}
            {course.isOwner && (
              <>
                <Link to={"/instructor/course/" + course.id} className="btn btn-primary btn-block">
                  Edit course
                </Link>
                <Link to={"/learn/" + course.id} className="btn btn-outline btn-block">
                  Preview as student
                </Link>
              </>
            )}

            {/* 2. The user can already watch the course */}
            {!course.isOwner && course.canWatch && (
              <>
                <p className="success-text">
                  {course.isEnrolled ? "✓ You are enrolled in this course" : "✓ You can watch this course"}
                </p>
                <Link to={"/learn/" + course.id} className="btn btn-primary btn-block">
                  Go to course
                </Link>
              </>
            )}

            {/* 3. Subscription expired */}
            {!course.isOwner && subscriptionExpired && (
              <div className="alert alert-warning">
                You enrolled in this course with a subscription that has expired. Renew your subscription or buy the
                course to continue.
              </div>
            )}

            {/* 4. The user cannot watch the course yet */}
            {!course.isOwner && !course.canWatch && (
              <>
                {isFree && (
                  <button className="btn btn-primary btn-block" onClick={handleEnroll} disabled={working}>
                    {working ? "Enrolling..." : "Enroll now - it's free"}
                  </button>
                )}

                {!isFree && course.hasActiveSubscription && (
                  <button className="btn btn-primary btn-block" onClick={handleEnroll} disabled={working}>
                    {working ? "Enrolling..." : "Start learning (included in your plan)"}
                  </button>
                )}

                {!isFree && (
                  <button
                    className={course.hasActiveSubscription ? "btn btn-outline btn-block" : "btn btn-primary btn-block"}
                    onClick={handleBuy}
                  >
                    Buy this course
                  </button>
                )}

                {!isFree && !course.hasActiveSubscription && (
                  <div className="subscribe-hint">
                    <p>
                      <strong>Or subscribe</strong> and get this course plus every other course.
                    </p>
                    <Link to="/plans" className="btn btn-outline btn-block">
                      {subscriptionExpired ? "Renew subscription" : "See subscription plans"}
                    </Link>
                  </div>
                )}
              </>
            )}

            <h4>This course includes:</h4>
            <ul className="includes-list">
              <li>🎬 {formatDuration(course.totalDurationSeconds)} on-demand video</li>
              <li>📚 {course.lectureCount} lectures</li>
              <li>📈 Progress tracking</li>
              <li>🏆 Certificate of completion</li>
            </ul>
          </div>
        </aside>
      </div>

      {/* Free preview popup */}
      {previewLecture && (
        <div className="modal-backdrop" onClick={() => setPreviewLecture(null)}>
          <div className="modal modal-dark" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <span>
                Course preview: <strong>{previewLecture.title}</strong>
              </span>
              <button className="close-button" onClick={() => setPreviewLecture(null)}>
                ✕
              </button>
            </div>
            <video src={previewLecture.videoUrl} controls autoPlay className="preview-video" />
          </div>
        </div>
      )}
    </div>
  );
}

export default CourseDetailPage;
