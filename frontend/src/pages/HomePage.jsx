import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import {
  ArrowRight,
  Award,
  Briefcase,
  Camera,
  CheckCircle2,
  Clock,
  Code2,
  Layers,
  Megaphone,
  Monitor,
  Music,
  Palette,
  PlayCircle,
  Search,
  Sparkles,
  Sprout,
  TrendingUp,
  Users,
} from "lucide-react";
import api from "../api";
import CourseCard from "../components/CourseCard";
import InstructorCard from "../components/InstructorCard";
import { CourseGridSkeleton } from "../components/Skeleton";
import { useAuth } from "../AuthContext";
import { formatPrice } from "../utils";

// An icon for each category name. Categories not in this list get the Layers icon.
const CATEGORY_ICONS = {
  Development: Code2,
  Business: Briefcase,
  Design: Palette,
  Marketing: Megaphone,
  "IT & Software": Monitor,
  "Personal Development": Sprout,
  "Photography & Video": Camera,
  Music: Music,
};

const FEATURES = [
  { icon: PlayCircle, title: "Learn by watching", text: "Short video lectures, organised section by section." },
  { icon: TrendingUp, title: "Track your progress", text: "Every lecture you finish is saved. Continue anytime." },
  { icon: Award, title: "Earn certificates", text: "Complete a course and get a printable certificate." },
  { icon: Users, title: "Expert instructors", text: "Learn from people who do this work every day." },
];

function HomePage() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [popularCourses, setPopularCourses] = useState([]);
  const [newCourses, setNewCourses] = useState([]);
  const [categories, setCategories] = useState([]);
  const [cheapestPlan, setCheapestPlan] = useState(null);
  const [instructors, setInstructors] = useState([]);
  const [loading, setLoading] = useState(true);
  const [searchText, setSearchText] = useState("");

  useEffect(function () {
    async function loadData() {
      try {
        // Promise.all runs the 5 requests at the same time, which is faster.
        const results = await Promise.all([
          api.get("/courses?sort=popular&pageSize=8"),
          api.get("/courses?sort=newest&pageSize=4"),
          api.get("/categories"),
          api.get("/plans"),
          api.get("/instructors?limit=4"),
        ]);
        setInstructors(results[4].data);
        setPopularCourses(results[0].data.items);
        setNewCourses(results[1].data.items);
        setCategories(results[2].data);
        if (results[3].data.length > 0) {
          setCheapestPlan(results[3].data[0]); // plans come sorted by price
        }
      } catch (error) {
        console.log(error);
      }
      setLoading(false);
    }
    loadData();
  }, []);

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

  function handleSearch(event) {
    event.preventDefault();
    navigate("/courses?search=" + encodeURIComponent(searchText.trim()));
  }

  return (
    <div>
      {/* ---------- Hero ---------- */}
      <section className="hero">
        <div className="hero-blob hero-blob-1"></div>
        <div className="hero-blob hero-blob-2"></div>
        <div className="hero-blob hero-blob-3"></div>

        <div className="container hero-inner">
          <div className="hero-text animate-in">
            <span className="eyebrow">
              <Sparkles size={16} /> Online learning, made simple
            </span>
            <h1>
              {user ? (
                <>
                  Welcome back, <span className="gradient-text">{user.fullName.split(" ")[0]}</span>
                </>
              ) : (
                <>
                  Learn the skills that <span className="gradient-text">move you forward</span>
                </>
              )}
            </h1>
            <p>
              Video courses from expert instructors. Learn at your own pace, track every lecture, and earn a
              certificate when you finish.
            </p>

            <form className="hero-search" onSubmit={handleSearch}>
              <Search size={20} />
              <input
                type="text"
                placeholder="What do you want to learn?"
                value={searchText}
                onChange={(e) => setSearchText(e.target.value)}
              />
              <button type="submit" className="btn btn-primary">
                Search
              </button>
            </form>

            <div className="hero-points">
              <span>
                <CheckCircle2 size={18} /> Free courses available
              </span>
              <span>
                <CheckCircle2 size={18} /> Learn at your own pace
              </span>
            </div>
          </div>

          {/* Decorative floating cards */}
          <div className="hero-art" aria-hidden="true">
            <div className="float-card float-card-main">
              <div className="float-card-image">
                <PlayCircle size={48} />
              </div>
              <strong>React - The Practical Guide</strong>
              <div className="float-progress">
                <div></div>
              </div>
              <small>68% complete</small>
            </div>
            <div className="float-card float-card-small float-card-top">
              <Award size={22} />
              <div>
                <strong>Certificate earned</strong>
                <small>C# for Beginners</small>
              </div>
            </div>
            <div className="float-card float-card-small float-card-bottom">
              <Clock size={22} />
              <div>
                <strong>12 min left</strong>
                <small>Lecture 5 · Hooks</small>
              </div>
            </div>
          </div>
        </div>
      </section>

      <div className="container">
        {/* ---------- Categories ---------- */}
        <section className="home-section">
          <div className="section-heading">
            <div>
              <h2>Top categories</h2>
              <p>Find the right course for your goals.</p>
            </div>
          </div>
          <div className="category-grid">
            {categories.map(function (category, index) {
              const Icon = CATEGORY_ICONS[category.name] || Layers;
              return (
                <Link
                  key={category.id}
                  to={"/courses?categoryId=" + category.id}
                  className="category-tile stagger-item"
                  style={{ "--i": index }}
                >
                  <span className="category-icon">
                    <Icon size={22} />
                  </span>
                  <span>
                    <strong>{category.name}</strong>
                    <small>
                      {category.courseCount} {category.courseCount === 1 ? "course" : "courses"}
                    </small>
                  </span>
                </Link>
              );
            })}
          </div>
        </section>

        {/* ---------- Popular courses ---------- */}
        <section className="home-section">
          <div className="section-heading">
            <div>
              <h2>Students are viewing</h2>
              <p>The most popular courses right now.</p>
            </div>
            <Link to="/courses" className="see-all">
              See all courses <ArrowRight size={16} />
            </Link>
          </div>
          {loading ? (
            <CourseGridSkeleton count={4} />
          ) : (
            <div className="course-grid">
              {popularCourses.map(function (course, index) {
                return <CourseCard key={course.id} course={course} index={index} />;
              })}
            </div>
          )}
          {!loading && popularCourses.length === 0 && (
            <p className="muted">No courses yet. Instructors can publish the first one!</p>
          )}
        </section>

        {/* ---------- Top instructors ---------- */}
        {!loading && instructors.length > 0 && (
          <section className="home-section">
            <div className="section-heading">
              <div>
                <h2>Learn from top instructors</h2>
                <p>Follow an instructor to hear about their new courses first.</p>
              </div>
            </div>
            <div className="instructor-grid">
              {instructors.map(function (instructor, index) {
                return (
                  <InstructorCard
                    key={instructor.id}
                    instructor={instructor}
                    index={index}
                    onFollowChange={handleFollowChange}
                  />
                );
              })}
            </div>
          </section>
        )}

        {/* ---------- Features ---------- */}
        <section className="home-section">
          <div className="section-heading section-heading-center">
            <div>
              <h2>Why learn with SmartLearn?</h2>
              <p>Everything you need to learn a new skill, in one place.</p>
            </div>
          </div>
          <div className="feature-grid">
            {FEATURES.map(function (feature, index) {
              const Icon = feature.icon;
              return (
                <div key={feature.title} className="feature-card stagger-item" style={{ "--i": index }}>
                  <span className="feature-icon">
                    <Icon size={24} />
                  </span>
                  <h3>{feature.title}</h3>
                  <p>{feature.text}</p>
                </div>
              );
            })}
          </div>
        </section>

        {/* ---------- Subscription banner ---------- */}
        {cheapestPlan && (
          <section className="plan-banner">
            <div className="header-glow header-glow-1"></div>
            <div className="plan-banner-text">
              <span className="eyebrow eyebrow-light">
                <Sparkles size={16} /> Personal Plan
              </span>
              <h2>One subscription. Every course.</h2>
              <p>
                Get unlimited access to all courses. Plans start at <strong>{formatPrice(cheapestPlan.price)}</strong>{" "}
                for {cheapestPlan.durationDays} days.
              </p>
            </div>
            <Link to="/plans" className="btn btn-white btn-large">
              See plans <ArrowRight size={18} />
            </Link>
          </section>
        )}

        {/* ---------- Newest courses ---------- */}
        {!loading && newCourses.length > 0 && (
          <section className="home-section">
            <div className="section-heading">
              <div>
                <h2>New and noteworthy</h2>
                <p>Fresh courses from our instructors.</p>
              </div>
            </div>
            <div className="course-grid">
              {newCourses.map(function (course, index) {
                return <CourseCard key={course.id} course={course} index={index} />;
              })}
            </div>
          </section>
        )}

        {/* ---------- Become an instructor ---------- */}
        {!user && (
          <section className="teach-banner">
            <div className="teach-icon">
              <Users size={40} />
            </div>
            <div>
              <h2>Become an instructor</h2>
              <p>Create a course, upload your videos section by section, and teach students everywhere.</p>
              <Link to="/register?role=Instructor" className="btn btn-dark">
                Start teaching today <ArrowRight size={18} />
              </Link>
            </div>
          </section>
        )}
      </div>
    </div>
  );
}

export default HomePage;
