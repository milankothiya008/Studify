import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import api from "../api";
import CourseCard from "../components/CourseCard";
import Spinner from "../components/Spinner";
import { useAuth } from "../AuthContext";
import { formatPrice } from "../utils";

function HomePage() {
  const { user } = useAuth();
  const [popularCourses, setPopularCourses] = useState([]);
  const [newCourses, setNewCourses] = useState([]);
  const [categories, setCategories] = useState([]);
  const [cheapestPlan, setCheapestPlan] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(function () {
    async function loadData() {
      try {
        const popular = await api.get("/courses?sort=popular&pageSize=8");
        const newest = await api.get("/courses?sort=newest&pageSize=4");
        const categoryList = await api.get("/categories");
        const plans = await api.get("/plans");

        setPopularCourses(popular.data.items);
        setNewCourses(newest.data.items);
        setCategories(categoryList.data);
        if (plans.data.length > 0) {
          setCheapestPlan(plans.data[0]); // plans come sorted by price
        }
      } catch (error) {
        console.log(error);
      }
      setLoading(false);
    }
    loadData();
  }, []);

  return (
    <div>
      {/* Big banner at the top */}
      <section className="hero">
        <div className="container hero-inner">
          <div className="hero-text">
            <h1>{user ? "Welcome back, " + user.fullName.split(" ")[0] + "!" : "Learn skills for your future"}</h1>
            <p>
              Courses from expert instructors. Learn at your own pace, track your progress, and get a certificate
              when you finish.
            </p>
            <div className="hero-buttons">
              <Link to="/courses" className="btn btn-primary btn-large">
                Explore courses
              </Link>
              {!user && (
                <Link to="/register" className="btn btn-outline-light btn-large">
                  Join for free
                </Link>
              )}
            </div>
          </div>
          <div className="hero-art">🎓</div>
        </div>
      </section>

      <div className="container page">
        {/* Categories */}
        <h2 className="section-title">Top categories</h2>
        <div className="category-chips">
          {categories.map(function (category) {
            return (
              <Link key={category.id} to={"/courses?categoryId=" + category.id} className="chip">
                {category.name}
              </Link>
            );
          })}
        </div>

        {loading && <Spinner />}

        {/* Popular courses */}
        {!loading && (
          <>
            <h2 className="section-title">Students are viewing</h2>
            <div className="course-grid">
              {popularCourses.map(function (course) {
                return <CourseCard key={course.id} course={course} />;
              })}
            </div>
            {popularCourses.length === 0 && <p className="muted">No courses yet. Instructors can publish the first one!</p>}
          </>
        )}

        {/* Subscription banner */}
        {cheapestPlan && (
          <section className="plan-banner">
            <div>
              <h2>SmartLearn Personal Plan</h2>
              <p>
                Get access to <strong>every course</strong> with one subscription. Plans start at{" "}
                <strong>{formatPrice(cheapestPlan.price)}</strong> for {cheapestPlan.durationDays} days.
              </p>
            </div>
            <Link to="/plans" className="btn btn-primary btn-large">
              See plans
            </Link>
          </section>
        )}

        {/* Newest courses */}
        {!loading && newCourses.length > 0 && (
          <>
            <h2 className="section-title">New and noteworthy</h2>
            <div className="course-grid">
              {newCourses.map(function (course) {
                return <CourseCard key={course.id} course={course} />;
              })}
            </div>
          </>
        )}

        {/* Become an instructor */}
        {!user && (
          <section className="teach-banner">
            <div className="teach-art">🧑‍🏫</div>
            <div>
              <h2>Become an instructor</h2>
              <p>Create a course, upload your videos section by section, and teach students around the world.</p>
              <Link to="/register?role=Instructor" className="btn btn-dark">
                Start teaching today
              </Link>
            </div>
          </section>
        )}
      </div>
    </div>
  );
}

export default HomePage;
