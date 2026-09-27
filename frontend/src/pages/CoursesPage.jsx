import { useEffect, useState } from "react";
import { useSearchParams } from "react-router-dom";
import api from "../api";
import CourseCard from "../components/CourseCard";
import Spinner from "../components/Spinner";

const LEVELS = ["Beginner", "Intermediate", "Advanced", "All Levels"];
const PAGE_SIZE = 12;

// Browse and search courses. All filters are kept in the address bar,
// e.g. /courses?search=react&level=Beginner&page=2
function CoursesPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const [result, setResult] = useState({ items: [], totalCount: 0 });
  const [categories, setCategories] = useState([]);
  const [loading, setLoading] = useState(true);

  const search = searchParams.get("search") || "";
  const categoryId = searchParams.get("categoryId") || "";
  const level = searchParams.get("level") || "";
  const price = searchParams.get("price") || "";
  const sort = searchParams.get("sort") || "popular";
  const page = Number(searchParams.get("page") || 1);

  useEffect(function () {
    api.get("/categories").then(function (response) {
      setCategories(response.data);
    });
  }, []);

  // Load the courses again every time a filter changes.
  useEffect(
    function () {
      setLoading(true);
      api
        .get("/courses", {
          params: {
            search: search,
            categoryId: categoryId || null,
            level: level,
            price: price,
            sort: sort,
            page: page,
            pageSize: PAGE_SIZE,
          },
        })
        .then(function (response) {
          setResult(response.data);
        })
        .finally(function () {
          setLoading(false);
        });
    },
    [search, categoryId, level, price, sort, page]
  );

  // Changes one filter and goes back to page 1.
  function changeFilter(name, value) {
    const newParams = new URLSearchParams(searchParams);
    if (value) {
      newParams.set(name, value);
    } else {
      newParams.delete(name);
    }
    newParams.delete("page");
    setSearchParams(newParams);
  }

  function goToPage(newPage) {
    const newParams = new URLSearchParams(searchParams);
    newParams.set("page", newPage);
    setSearchParams(newParams);
    window.scrollTo(0, 0);
  }

  const totalPages = Math.ceil(result.totalCount / PAGE_SIZE);

  let heading = "All courses";
  if (search) {
    heading = result.totalCount + ' results for "' + search + '"';
  } else if (categoryId) {
    const category = categories.find((c) => String(c.id) === categoryId);
    if (category) {
      heading = category.name + " courses";
    }
  }

  return (
    <div className="container page">
      <h1>{heading}</h1>

      <div className="browse-layout">
        {/* Filters on the left */}
        <aside className="filters">
          <div className="filter-group">
            <h4>Sort by</h4>
            <select value={sort} onChange={(e) => changeFilter("sort", e.target.value)}>
              <option value="popular">Most popular</option>
              <option value="rating">Highest rated</option>
              <option value="newest">Newest</option>
              <option value="price-low">Price: low to high</option>
              <option value="price-high">Price: high to low</option>
            </select>
          </div>

          <div className="filter-group">
            <h4>Category</h4>
            <label>
              <input type="radio" checked={categoryId === ""} onChange={() => changeFilter("categoryId", "")} />
              All categories
            </label>
            {categories.map(function (category) {
              return (
                <label key={category.id}>
                  <input
                    type="radio"
                    checked={categoryId === String(category.id)}
                    onChange={() => changeFilter("categoryId", String(category.id))}
                  />
                  {category.name} <span className="muted">({category.courseCount})</span>
                </label>
              );
            })}
          </div>

          <div className="filter-group">
            <h4>Level</h4>
            <label>
              <input type="radio" checked={level === ""} onChange={() => changeFilter("level", "")} />
              Any level
            </label>
            {LEVELS.map(function (item) {
              return (
                <label key={item}>
                  <input type="radio" checked={level === item} onChange={() => changeFilter("level", item)} />
                  {item}
                </label>
              );
            })}
          </div>

          <div className="filter-group">
            <h4>Price</h4>
            <label>
              <input type="radio" checked={price === ""} onChange={() => changeFilter("price", "")} />
              Any price
            </label>
            <label>
              <input type="radio" checked={price === "free"} onChange={() => changeFilter("price", "free")} />
              Free
            </label>
            <label>
              <input type="radio" checked={price === "paid"} onChange={() => changeFilter("price", "paid")} />
              Paid
            </label>
          </div>

          {(search || categoryId || level || price) && (
            <button className="btn btn-outline btn-block" onClick={() => setSearchParams({})}>
              Clear filters
            </button>
          )}
        </aside>

        {/* Courses on the right */}
        <section>
          {loading && <Spinner />}

          {!loading && result.items.length === 0 && (
            <div className="empty-state">
              <h3>No courses found</h3>
              <p>Try other words or remove some filters.</p>
            </div>
          )}

          {!loading && (
            <div className="course-grid course-grid-3">
              {result.items.map(function (course) {
                return <CourseCard key={course.id} course={course} />;
              })}
            </div>
          )}

          {totalPages > 1 && (
            <div className="pagination">
              <button className="btn btn-outline" disabled={page <= 1} onClick={() => goToPage(page - 1)}>
                ‹ Previous
              </button>
              <span>
                Page {page} of {totalPages}
              </span>
              <button className="btn btn-outline" disabled={page >= totalPages} onClick={() => goToPage(page + 1)}>
                Next ›
              </button>
            </div>
          )}
        </section>
      </div>
    </div>
  );
}

export default CoursesPage;
