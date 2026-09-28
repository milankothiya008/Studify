import { useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import {
  AlertTriangle,
  Award,
  BarChart3,
  BookOpen,
  Calendar,
  Check,
  CheckCircle2,
  ChevronDown,
  ClipboardCheck,
  Globe,
  Infinity as InfinityIcon,
  PlayCircle,
  SearchX,
  Star,
  TrendingUp,
  Users,
  X,
} from "lucide-react";
import api, { getErrorMessage } from "../api";
import { useAuth } from "../AuthContext";
import { useToast } from "../ToastContext";
import StarRating from "../components/StarRating";
import Avatar from "../components/Avatar";
import Spinner from "../components/Spinner";
import EmptyState from "../components/EmptyState";
import WishlistButton from "../components/WishlistButton";
import { formatClock, formatDate, formatDuration, formatPrice, splitLines } from "../utils";

// The course landing page (like a Udemy course page).
function CourseDetailPage() {
  const { id } = useParams();
  const { user } = useAuth();
  const showToast = useToast();
  const navigate = useNavigate();

  const [course, setCourse] = useState(null);
  const [reviews, setReviews] = useState([]);
  const [error, setError] = useState("");
  const [working, setWorking] = useState(false);
  const [openSections, setOpenSections] = useState({}); // which sections are expanded
  const [previewLecture, setPreviewLecture] = useState(null); // lecture shown in the popup

  useEffect(
    function () {
      async function loadCourse() {
        try {
          const results = await Promise.all([api.get("/courses/" + id), api.get("/courses/" + id + "/reviews")]);
          const courseData = results[0].data;
          setCourse(courseData);
          setReviews(results[1].data);

          // Open the first section by default.
          if (courseData.sections.length > 0) {
            const open = {};
            open[courseData.sections[0].id] = true;
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

  function toggleAll() {
    const allOpen = course.sections.every((section) => openSections[section.id]);
    const newState = {};
    course.sections.forEach(function (section) {
      newState[section.id] = !allOpen;
    });
    setOpenSections(newState);
  }

  // Free course, or paid course with an active subscription.
  async function handleEnroll() {
    if (!user) {
      navigate("/login", { state: { from: "/course/" + id } });
      return;
    }

    setWorking(true);
    try {
      const response = await api.post("/enrollments/" + id);
      showToast(response.data.message);
      navigate("/learn/" + id);
    } catch (err) {
      showToast(getErrorMessage(err), "error");
      setWorking(false);
    }
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
        <EmptyState icon={SearchX} title={error} text="The course may have been removed or unpublished.">
          <Link to="/courses" className="btn btn-primary">
            Browse courses
          </Link>
        </EmptyState>
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
  const allSectionsOpen = course.sections.length > 0 && course.sections.every((section) => openSections[section.id]);

  // How many reviews gave 5 stars, 4 stars ... (for the bars in the reviews section)
  const starCounts = [0, 0, 0, 0, 0, 0];
  reviews.forEach(function (review) {
    starCounts[review.rating]++;
  });

  return (
    <div>
      {/* ---------- Dark header ---------- */}
      <section className="course-header">
        <div className="header-glow header-glow-1"></div>
        <div className="container course-header-inner">
          <div className="course-header-text animate-in">
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
                <Users size={16} /> {course.studentCount} {course.studentCount === 1 ? "student" : "students"}
              </span>
              <span>
                <BarChart3 size={16} /> {course.level}
              </span>
            </div>
            <p className="course-meta">
              <span>
                Created by <strong>{course.instructorName}</strong>
              </span>
              <span>
                <Calendar size={16} /> Updated {formatDate(course.updatedAt)}
              </span>
              <span>
                <Globe size={16} /> {course.language}
              </span>
            </p>
            {!course.isPublished && (
              <span className="badge badge-warning">
                <AlertTriangle size={14} /> Not published - only you and enrolled students can see this page
              </span>
            )}
          </div>
        </div>
      </section>

      <div className="container course-body">
        <div className="course-main">
          {/* ---------- What you'll learn ---------- */}
          {learnItems.length > 0 && (
            <div className="card card-padded">
              <h2>What you'll learn</h2>
              <ul className="check-list two-columns">
                {learnItems.map(function (item, index) {
                  return (
                    <li key={index}>
                      <Check size={18} /> {item}
                    </li>
                  );
                })}
              </ul>
            </div>
          )}

          {/* ---------- Curriculum ---------- */}
          <section className="course-section">
            <h2>Course content</h2>
            <div className="curriculum-summary">
              <span>
                {course.sections.length} sections · {course.lectureCount} lectures ·{" "}
                {formatDuration(course.totalDurationSeconds)} total length
              </span>
              {course.sections.length > 0 && (
                <button className="link-button" onClick={toggleAll}>
                  {allSectionsOpen ? "Collapse all sections" : "Expand all sections"}
                </button>
              )}
            </div>

            <div className="accordion">
              {course.sections.map(function (section) {
                const sectionSeconds = section.lectures.reduce((sum, l) => sum + l.durationSeconds, 0);
                const isOpen = Boolean(openSections[section.id]);
                return (
                  <div key={section.id} className={isOpen ? "accordion-item open" : "accordion-item"}>
                    <button className="accordion-header" onClick={() => toggleSection(section.id)}>
                      <span className="accordion-title">
                        <ChevronDown size={18} className="accordion-chevron" /> {section.title}
                      </span>
                      <span className="muted small">
                        {section.lectures.length} lectures · {formatDuration(sectionSeconds)}
                      </span>
                    </button>

                    {/* The body is always rendered; CSS animates it open and closed. */}
                    <div className="accordion-body">
                      <ul className="lecture-list">
                        {section.lectures.map(function (lecture) {
                          const canPreview = lecture.isFreePreview && lecture.videoUrl;
                          return (
                            <li key={lecture.id}>
                              <span className="lecture-name">
                                <PlayCircle size={16} /> {lecture.title}
                              </span>
                              <span className="lecture-right">
                                {canPreview && (
                                  <button className="link-button" onClick={() => setPreviewLecture(lecture)}>
                                    Preview
                                  </button>
                                )}
                                <span className="muted">{formatClock(lecture.durationSeconds)}</span>
                              </span>
                            </li>
                          );
                        })}
                        {section.quiz && (
                          <li className="quiz-row">
                            <span className="lecture-name">
                              <ClipboardCheck size={16} /> Quiz: {section.quiz.title}
                            </span>
                            <span className="muted">{section.quiz.questionCount} questions</span>
                          </li>
                        )}
                      </ul>
                    </div>
                  </div>
                );
              })}
            </div>
          </section>

          {/* ---------- Requirements ---------- */}
          {requirementItems.length > 0 && (
            <section className="course-section">
              <h2>Requirements</h2>
              <ul className="bullet-list">
                {requirementItems.map(function (item, index) {
                  return <li key={index}>{item}</li>;
                })}
              </ul>
            </section>
          )}

          {/* ---------- Description ---------- */}
          {course.description && (
            <section className="course-section">
              <h2>Description</h2>
              <p className="pre-line">{course.description}</p>
            </section>
          )}

          {/* ---------- Instructor ---------- */}
          <section className="course-section">
            <h2>Your instructor</h2>
            <div className="card card-padded instructor-block">
              <Avatar name={course.instructorName} imageUrl={course.instructorImageUrl} size={88} />
              <div>
                <h3>{course.instructorName}</h3>
                {course.instructorHeadline && <p className="muted">{course.instructorHeadline}</p>}
                {course.instructorBio && <p className="pre-line">{course.instructorBio}</p>}
              </div>
            </div>
          </section>

          {/* ---------- Reviews ---------- */}
          <section className="course-section">
            <h2>Student reviews</h2>
            {reviews.length === 0 ? (
              <p className="muted">No reviews yet. Enrolled students can rate this course from the course player.</p>
            ) : (
              <>
                <div className="rating-summary card card-padded">
                  <div className="rating-big">
                    <strong>{course.averageRating.toFixed(1)}</strong>
                    <StarRating rating={course.averageRating} size={18} />
                    <span className="muted small">Course rating</span>
                  </div>
                  <div className="rating-bars">
                    {[5, 4, 3, 2, 1].map(function (stars) {
                      const percent = Math.round((starCounts[stars] * 100) / reviews.length);
                      return (
                        <div key={stars} className="rating-bar-row">
                          <div className="rating-bar">
                            <div style={{ width: percent + "%" }}></div>
                          </div>
                          <span className="rating-bar-label">
                            <Star size={13} className="star-icon filled" /> {stars}
                          </span>
                          <span className="muted small">{percent}%</span>
                        </div>
                      );
                    })}
                  </div>
                </div>

                <div className="review-list">
                  {reviews.map(function (review, index) {
                    return (
                      <div key={review.id} className="review stagger-item" style={{ "--i": index }}>
                        <Avatar name={review.userName} imageUrl={review.userImageUrl} size={44} />
                        <div>
                          <strong>{review.userName}</strong>
                          <div className="review-meta">
                            <StarRating rating={review.rating} />
                            <span className="muted small">{formatDate(review.createdAt)}</span>
                          </div>
                          {review.comment && <p>{review.comment}</p>}
                        </div>
                      </div>
                    );
                  })}
                </div>
              </>
            )}
          </section>
        </div>

        {/* ---------- Buy / enroll card ---------- */}
        <aside className="buy-card">
          <div className="buy-card-media">
            {course.promoVideoUrl ? (
              <video src={course.promoVideoUrl} poster={course.thumbnailUrl} controls preload="metadata" />
            ) : course.thumbnailUrl ? (
              <img src={course.thumbnailUrl} alt={course.title} />
            ) : (
              <div className="no-image">
                <PlayCircle size={48} />
              </div>
            )}
          </div>

          <div className="buy-card-body">
            {!course.canWatch && (
              <div className={isFree ? "buy-price price-free" : "buy-price"}>{formatPrice(course.price)}</div>
            )}

            {/* 1. The instructor of this course */}
            {course.isOwner && (
              <>
                <Link to={"/instructor/course/" + course.id} className="btn btn-primary btn-block btn-large">
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
                  <CheckCircle2 size={18} />
                  {course.isEnrolled ? "You are enrolled in this course" : "You can watch this course"}
                </p>
                <Link to={"/learn/" + course.id} className="btn btn-primary btn-block btn-large">
                  Go to course
                </Link>
              </>
            )}

            {/* 3. Subscription expired */}
            {!course.isOwner && subscriptionExpired && (
              <div className="alert alert-warning">
                <AlertTriangle size={18} />
                <span>
                  You joined this course with a subscription that has expired. Renew it or buy the course to continue.
                </span>
              </div>
            )}

            {/* 4. The user cannot watch the course yet */}
            {!course.isOwner && !course.canWatch && (
              <>
                {isFree && (
                  <button className="btn btn-primary btn-block btn-large" onClick={handleEnroll} disabled={working}>
                    {working && <span className="btn-spinner"></span>}
                    {working ? "Enrolling..." : "Enroll now - it's free"}
                  </button>
                )}

                {!isFree && course.hasActiveSubscription && (
                  <button className="btn btn-primary btn-block btn-large" onClick={handleEnroll} disabled={working}>
                    {working && <span className="btn-spinner"></span>}
                    {working ? "Enrolling..." : "Start learning (included in your plan)"}
                  </button>
                )}

                {!isFree && (
                  <button
                    className={
                      course.hasActiveSubscription ? "btn btn-outline btn-block" : "btn btn-primary btn-block btn-large"
                    }
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

            {!course.isOwner && !course.canWatch && <WishlistButton courseId={course.id} variant="button" />}

            <h4>This course includes</h4>
            <ul className="includes-list">
              <li>
                <PlayCircle size={18} /> {formatDuration(course.totalDurationSeconds)} on-demand video
              </li>
              <li>
                <BookOpen size={18} /> {course.lectureCount} lectures
              </li>
              <li>
                <TrendingUp size={18} /> Progress tracking
              </li>
              <li>
                <InfinityIcon size={18} /> Learn at your own pace
              </li>
              <li>
                <Award size={18} /> Certificate of completion
              </li>
            </ul>
          </div>
        </aside>
      </div>

      {/* ---------- Free preview popup ---------- */}
      {previewLecture && (
        <div className="modal-backdrop" onClick={() => setPreviewLecture(null)}>
          <div className="modal modal-dark" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <span>
                <small>Course preview</small>
                <strong>{previewLecture.title}</strong>
              </span>
              <button className="icon-button icon-button-light" onClick={() => setPreviewLecture(null)} aria-label="Close">
                <X size={22} />
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
