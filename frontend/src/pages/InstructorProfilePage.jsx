import { useEffect, useState } from "react";
import { Link, useParams, useSearchParams } from "react-router-dom";
import {
  Award,
  BookOpen,
  CalendarDays,
  Check,
  GraduationCap,
  Link2,
  MessageSquareQuote,
  Pencil,
  Quote,
  Star,
  UserRoundSearch,
  Users,
} from "lucide-react";
import api, { getErrorMessage } from "../api";
import { useToast } from "../ToastContext";
import Avatar from "../components/Avatar";
import CountUp from "../components/CountUp";
import CourseCard from "../components/CourseCard";
import EmptyState from "../components/EmptyState";
import FollowButton from "../components/FollowButton";
import SocialLinks from "../components/SocialLinks";
import StarRating from "../components/StarRating";
import { SkeletonBlock } from "../components/Skeleton";
import { formatDate, timeAgo } from "../utils";

const TABS = ["courses", "about", "reviews", "followers"];

const SORTS = {
  popular: { label: "Most popular", compare: (a, b) => b.studentCount - a.studentCount },
  rating: { label: "Highest rated", compare: (a, b) => b.averageRating - a.averageRating },
  newest: { label: "Recently updated", compare: (a, b) => new Date(b.updatedAt) - new Date(a.updatedAt) },
  "price-low": { label: "Price: low to high", compare: (a, b) => a.price - b.price },
};

// The public profile of an instructor: who they are, their courses, reviews and followers.
function InstructorProfilePage() {
  const { id } = useParams();
  const showToast = useToast();
  const [searchParams, setSearchParams] = useSearchParams();

  const [profile, setProfile] = useState(null);
  const [error, setError] = useState("");
  const [followers, setFollowers] = useState(null); // loaded when the Followers tab opens
  const [sort, setSort] = useState("popular");
  const [bioOpen, setBioOpen] = useState(false);
  const [copied, setCopied] = useState(false);

  // The open tab is kept in the address (?tab=reviews), so links can open a tab directly.
  const tabInUrl = searchParams.get("tab");
  const activeTab = TABS.includes(tabInUrl) ? tabInUrl : "courses";

  useEffect(
    function () {
      setProfile(null);
      setFollowers(null);
      setError("");
      api
        .get("/instructors/" + id)
        .then(function (response) {
          setProfile(response.data);
        })
        .catch(function (err) {
          setError(getErrorMessage(err));
        });
    },
    [id]
  );

  useEffect(
    function () {
      if (activeTab === "followers" && followers === null) {
        api
          .get("/instructors/" + id + "/followers")
          .then((response) => setFollowers(response.data))
          .catch((err) => showToast(getErrorMessage(err), "error"));
      }
    },
    [activeTab, followers, id]
  );

  function openTab(tab) {
    const params = new URLSearchParams(searchParams);
    if (tab === "courses") {
      params.delete("tab");
    } else {
      params.set("tab", tab);
    }
    setSearchParams(params, { replace: true });
  }

  function handleFollowChange(result) {
    setProfile({ ...profile, isFollowing: result.isFollowing, followerCount: result.followerCount });
    setFollowers(null); // load the list again next time
  }

  async function copyLink() {
    try {
      await navigator.clipboard.writeText(window.location.origin + "/instructors/" + id);
      setCopied(true);
      showToast("Profile link copied.");
      setTimeout(() => setCopied(false), 2000);
    } catch {
      showToast("Could not copy the link.", "error");
    }
  }

  if (error) {
    return (
      <div className="container page">
        <EmptyState icon={UserRoundSearch} title={error} text="This person may not teach on SmartLearn anymore.">
          <Link to="/courses" className="btn btn-primary">
            Browse courses
          </Link>
        </EmptyState>
      </div>
    );
  }

  if (!profile) {
    return <ProfileSkeleton />;
  }

  const firstName = profile.fullName.split(" ")[0];
  const sortedCourses = [...profile.courses].sort(SORTS[sort].compare);
  const longBio = profile.bio && profile.bio.length > 420;

  // Categories this instructor teaches, most courses first.
  const categoryCounts = {};
  profile.courses.forEach(function (course) {
    if (course.categoryName) {
      categoryCounts[course.categoryName] = (categoryCounts[course.categoryName] || 0) + 1;
    }
  });
  const topics = Object.keys(categoryCounts).sort((a, b) => categoryCounts[b] - categoryCounts[a]);

  const stats = [
    { icon: Users, label: "Students", value: profile.studentCount },
    { icon: BookOpen, label: "Courses", value: profile.courseCount },
    { icon: Star, label: "Instructor rating", value: profile.averageRating, decimals: 1, empty: profile.reviewCount === 0 },
    { icon: MessageSquareQuote, label: "Reviews", value: profile.reviewCount },
    { icon: GraduationCap, label: "Followers", value: profile.followerCount },
  ];

  return (
    <div className="instructor-profile">
      {/* ---------- Hero ---------- */}
      <section className="profile-hero">
        <div className="header-glow header-glow-1"></div>
        <div className="header-glow header-glow-2"></div>
        <div className="container profile-hero-inner">
          <div className="profile-hero-avatar">
            <span className="avatar-ring avatar-ring-large">
              <Avatar name={profile.fullName} imageUrl={profile.profileImageUrl} size={148} />
            </span>
          </div>

          <div className="profile-hero-text">
            <span className="eyebrow">
              <Award size={15} /> Instructor
            </span>
            <h1>{profile.fullName}</h1>
            {profile.headline && <p className="profile-headline">{profile.headline}</p>}

            <div className="profile-meta">
              <span>
                <CalendarDays size={16} /> Teaching since {formatDate(profile.joinedAt)}
              </span>
              {topics.length > 0 && (
                <span>
                  <BookOpen size={16} /> {topics.slice(0, 3).join(" · ")}
                </span>
              )}
            </div>

            <div className="profile-actions">
              {profile.isMe ? (
                <Link to="/profile" className="btn btn-white">
                  <Pencil size={17} /> Edit profile
                </Link>
              ) : (
                <FollowButton
                  instructorId={profile.id}
                  isFollowing={profile.isFollowing}
                  onChange={handleFollowChange}
                  light
                />
              )}
              <button className="btn btn-glass" onClick={copyLink}>
                {copied ? <Check size={17} /> : <Link2 size={17} />} {copied ? "Copied" : "Share profile"}
              </button>
              <SocialLinks person={profile} light />
            </div>
          </div>
        </div>
      </section>

      {/* ---------- Numbers (they overlap the bottom of the hero) ---------- */}
      <div className="container">
        <div className="profile-stats">
          {stats.map(function (stat, index) {
            const Icon = stat.icon;
            return (
              <div key={stat.label} className="profile-stat stagger-item" style={{ "--i": index }}>
                <span className="profile-stat-icon">
                  <Icon size={20} />
                </span>
                <strong>{stat.empty ? "New" : <CountUp value={stat.value} decimals={stat.decimals} />}</strong>
                <span>{stat.label}</span>
              </div>
            );
          })}
        </div>
      </div>

      {/* ---------- Tabs ---------- */}
      <div className="container page profile-body">
        <div className="tabs profile-tabs" role="tablist">
          <TabButton name="courses" active={activeTab} onOpen={openTab} count={profile.courseCount}>
            Courses
          </TabButton>
          <TabButton name="about" active={activeTab} onOpen={openTab}>
            About
          </TabButton>
          <TabButton name="reviews" active={activeTab} onOpen={openTab} count={profile.reviewCount}>
            Reviews
          </TabButton>
          <TabButton name="followers" active={activeTab} onOpen={openTab} count={profile.followerCount}>
            Followers
          </TabButton>
        </div>

        {/* Courses */}
        {activeTab === "courses" && (
          <div className="tab-content" key="courses">
            {profile.courses.length === 0 ? (
              <EmptyState
                icon={BookOpen}
                title="No courses yet"
                text={profile.isMe ? "Publish a course and it will appear here." : firstName + " is still preparing a first course."}
              >
                {profile.isMe && (
                  <Link to="/instructor" className="btn btn-primary">
                    Go to dashboard
                  </Link>
                )}
              </EmptyState>
            ) : (
              <>
                <div className="profile-toolbar">
                  <h2>
                    Courses by {firstName} <span className="muted">({profile.courses.length})</span>
                  </h2>
                  <select className="select-small" value={sort} onChange={(e) => setSort(e.target.value)} aria-label="Sort courses">
                    {Object.keys(SORTS).map(function (key) {
                      return (
                        <option key={key} value={key}>
                          {SORTS[key].label}
                        </option>
                      );
                    })}
                  </select>
                </div>
                <div className="course-grid" key={sort}>
                  {sortedCourses.map(function (course, index) {
                    return <CourseCard key={course.id} course={course} index={index} />;
                  })}
                </div>
              </>
            )}
          </div>
        )}

        {/* About */}
        {activeTab === "about" && (
          <div className="tab-content profile-about" key="about">
            <div className="card card-padded profile-bio">
              <h2>About {firstName}</h2>
              {profile.bio ? (
                <>
                  <p className={longBio && !bioOpen ? "pre-line bio-text bio-clamped" : "pre-line bio-text"}>{profile.bio}</p>
                  {longBio && (
                    <button className="link-button" onClick={() => setBioOpen(!bioOpen)}>
                      {bioOpen ? "Show less" : "Show more"}
                    </button>
                  )}
                </>
              ) : (
                <p className="muted">{firstName} has not written a biography yet.</p>
              )}
            </div>

            <aside className="card card-padded profile-facts">
              <h3>At a glance</h3>
              <ul>
                <li>
                  <CalendarDays size={18} />
                  <span>
                    Joined <strong>{formatDate(profile.joinedAt)}</strong>
                  </span>
                </li>
                <li>
                  <Users size={18} />
                  <span>
                    <strong>{profile.studentCount.toLocaleString("en-IN")}</strong>{" "}
                    {profile.studentCount === 1 ? "student" : "students"} taught
                  </span>
                </li>
                <li>
                  <Star size={18} />
                  <span>
                    {profile.reviewCount > 0 ? (
                      <>
                        <strong>{profile.averageRating.toFixed(1)}</strong> average rating from {profile.reviewCount}{" "}
                        {profile.reviewCount === 1 ? "review" : "reviews"}
                      </>
                    ) : (
                      "No ratings yet"
                    )}
                  </span>
                </li>
              </ul>

              {topics.length > 0 && (
                <>
                  <h3>Teaches</h3>
                  <div className="topic-chips">
                    {topics.map(function (topic) {
                      return (
                        <span key={topic} className="topic-chip">
                          {topic}
                        </span>
                      );
                    })}
                  </div>
                </>
              )}

              {(profile.websiteUrl || profile.linkedInUrl || profile.youTubeUrl || profile.twitterUrl) && (
                <>
                  <h3>Find {firstName} online</h3>
                  <SocialLinks person={profile} />
                </>
              )}
            </aside>
          </div>
        )}

        {/* Reviews */}
        {activeTab === "reviews" && (
          <div className="tab-content" key="reviews">
            {profile.reviews.length === 0 ? (
              <EmptyState
                icon={MessageSquareQuote}
                title="No reviews yet"
                text={"Reviews that students write about " + firstName + "'s courses will appear here."}
              />
            ) : (
              <>
                <div className="profile-rating-banner card">
                  <strong>{profile.averageRating.toFixed(1)}</strong>
                  <div>
                    <StarRating rating={profile.averageRating} size={20} />
                    <span className="muted small">
                      Average of {profile.reviewCount} {profile.reviewCount === 1 ? "rating" : "ratings"} across all courses
                    </span>
                  </div>
                </div>
                <div className="profile-review-grid">
                  {profile.reviews.map(function (review, index) {
                    return (
                      <article key={review.id} className="profile-review card stagger-item" style={{ "--i": index }}>
                        <Quote size={28} className="profile-review-quote" />
                        <StarRating rating={review.rating} />
                        <p>{review.comment}</p>
                        <div className="profile-review-footer">
                          <Avatar name={review.userName} imageUrl={review.userImageUrl} size={38} />
                          <div>
                            <strong>{review.userName}</strong>
                            <span className="muted small">
                              {timeAgo(review.createdAt)} · <Link to={"/course/" + review.courseId}>{review.courseTitle}</Link>
                            </span>
                          </div>
                        </div>
                      </article>
                    );
                  })}
                </div>
              </>
            )}
          </div>
        )}

        {/* Followers */}
        {activeTab === "followers" && (
          <div className="tab-content" key="followers">
            {followers === null ? (
              <div className="follower-grid">
                {[0, 1, 2, 3, 4, 5].map((i) => (
                  <div key={i} className="follower card">
                    <SkeletonBlock height={48} width={48} round />
                    <div style={{ flex: 1 }}>
                      <SkeletonBlock height={14} width="70%" />
                      <SkeletonBlock height={10} width="45%" />
                    </div>
                  </div>
                ))}
              </div>
            ) : followers.length === 0 ? (
              <EmptyState
                icon={GraduationCap}
                title="No followers yet"
                text={
                  profile.isMe
                    ? "Students who follow you are told when you publish a new course."
                    : "Be the first to follow " + firstName + " and hear about new courses first."
                }
              >
                {!profile.isMe && (
                  <FollowButton instructorId={profile.id} isFollowing={profile.isFollowing} onChange={handleFollowChange} />
                )}
              </EmptyState>
            ) : (
              <>
                <p className="muted profile-followers-note">
                  Followers are told whenever {profile.isMe ? "you publish" : firstName + " publishes"} a new course.
                </p>
                <div className="follower-grid">
                  {followers.map(function (follower, index) {
                    const content = (
                      <>
                        <Avatar name={follower.fullName} imageUrl={follower.profileImageUrl} size={48} />
                        <div>
                          <strong>{follower.fullName}</strong>
                          <span className="muted small">
                            {follower.isInstructor ? "Instructor · " : ""}Following since {formatDate(follower.followedAt)}
                          </span>
                        </div>
                      </>
                    );
                    return follower.isInstructor ? (
                      <Link
                        key={follower.id}
                        to={"/instructors/" + follower.id}
                        className="follower card follower-link stagger-item"
                        style={{ "--i": index }}
                      >
                        {content}
                      </Link>
                    ) : (
                      <div key={follower.id} className="follower card stagger-item" style={{ "--i": index }}>
                        {content}
                      </div>
                    );
                  })}
                </div>
              </>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

function TabButton({ name, active, onOpen, count, children }) {
  return (
    <button
      role="tab"
      aria-selected={active === name}
      className={active === name ? "tab active" : "tab"}
      onClick={() => onOpen(name)}
    >
      {children}
      {count !== undefined && <span className="tab-count">{count}</span>}
    </button>
  );
}

// Grey placeholders in the shape of the page while it loads.
function ProfileSkeleton() {
  return (
    <div className="instructor-profile">
      <section className="profile-hero">
        <div className="container profile-hero-inner">
          <div className="skeleton skeleton-round profile-skeleton-avatar"></div>
          <div className="profile-hero-text" style={{ flex: 1 }}>
            <div className="skeleton skeleton-dark" style={{ height: 22, width: 110 }}></div>
            <div className="skeleton skeleton-dark" style={{ height: 40, width: "55%", marginTop: 16 }}></div>
            <div className="skeleton skeleton-dark" style={{ height: 18, width: "40%", marginTop: 12 }}></div>
          </div>
        </div>
      </section>
      <div className="container">
        <div className="profile-stats">
          {[0, 1, 2, 3, 4].map((i) => (
            <div key={i} className="profile-stat">
              <SkeletonBlock height={40} width={40} round />
              <SkeletonBlock height={24} width="50%" />
              <SkeletonBlock height={12} width="70%" />
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

export default InstructorProfilePage;
