import { useEffect, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { ChevronLeft, ChevronRight, SearchX, SlidersHorizontal, X } from "lucide-react";
import api from "../api";
import CourseCard from "../components/CourseCard";
import PageHeader from "../components/PageHeader";
import EmptyState from "../components/EmptyState";
import { CourseGridSkeleton } from "../components/Skeleton";

const LEVELS = ["Beginner", "Intermediate", "Advanced", "All Levels"];
const PAGE_SIZE = 12;

// Browse and search courses. All filters are kept in the address bar,
// e.g. /courses?search=react&level=Beginner&page=2
function CoursesPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const [result, setResult] = useState({ items: [], totalCount: 0 });
  const [categories, setCategories] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);
  const [filtersOpen, setFiltersOpen] = useState(false); // on phones the filters are hidden

  const search = searchParams.get("search") || "";
  const categoryId = searchParams.get("categoryId") || "";
  const level = searchParams.get("level") || "";
  const price = searchParams.get("price") || "";
  const sort = searchParams.get("sort") || "popular";

  // "?page=abc" or "?page=-3" -> page 1
  let page = parseInt(searchParams.get("page") || "1");
  if (isNaN(page) || page < 1) {
    page = 1;
  }

  useEffect(function () {
    api.get("/categories").then(function (response) {
      setCategories(response.data);
    });
  }, []);

  // Load the courses again every time a filter changes.
  useEffect(
    function () {
      // If the filters change again before this answer arrives, the old answer is ignored,
      // so the page never shows results for an older filter.
      let ignore = false;

      setLoading(true);
      setError(false);
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
          if (!ignore) {
            setResult(response.data);
          }
        })
        .catch(function () {
          if (!ignore) {
            setError(true);
          }
        })
        .finally(function () {
          if (!ignore) {
            setLoading(false);
          }
        });

      return function () {
        ignore = true;
      };
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
    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  const totalPages = Math.ceil(result.totalCount / PAGE_SIZE);
  const hasFilters = search || categoryId || level || price;

  let heading = "Explore courses";
  if (search) {
    heading = 'Results for "' + search + '"';
  } else if (categoryId) {
    const category = categories.find((c) => String(c.id) === categoryId);
    if (category) {
      heading = category.name + " courses";
    }
  }

  let subtitle = "Find your next course from our growing library.";
  if (!loading) {
    subtitle = result.totalCount + (result.totalCount === 1 ? " course found" : " courses found");
  }

  return (
    <div>
      <PageHeader title={heading} subtitle={subtitle} />

      <div className="container page">
        <div className="browse-toolbar">
          <button className="btn btn-outline btn-small show-mobile" onClick={() => setFiltersOpen(!filtersOpen)}>
            <SlidersHorizontal size={16} /> Filters
          </button>
          <div className="sort-box">
            <label htmlFor="sort">Sort by</label>
            <select id="sort" value={sort} onChange={(e) => changeFilter("sort", e.target.value)}>
              <option value="popular">Most popular</option>
              <option value="rating">Highest rated</option>
              <option value="newest">Newest</option>
              <option value="price-low">Price: low to high</option>
              <option value="price-high">Price: high to low</option>
            </select>
          </div>
        </div>

        <div className="browse-layout">
          {/* Filters on the left */}
          <aside className={filtersOpen ? "filters card open" : "filters card"}>
            <div className="filter-group">
              <h4>Category</h4>
              <label className="radio-row">
                <input type="radio" checked={categoryId === ""} onChange={() => changeFilter("categoryId", "")} />
                All categories
              </label>
              {categories.map(function (category) {
                return (
                  <label key={category.id} className="radio-row">
                    <input
                      type="radio"
                      checked={categoryId === String(category.id)}
                      onChange={() => changeFilter("categoryId", String(category.id))}
                    />
                    {category.name}
                    <span className="count">{category.courseCount}</span>
                  </label>
                );
              })}
            </div>

            <div className="filter-group">
              <h4>Level</h4>
              <label className="radio-row">
                <input type="radio" checked={level === ""} onChange={() => changeFilter("level", "")} />
                Any level
              </label>
              {LEVELS.map(function (item) {
                return (
                  <label key={item} className="radio-row">
                    <input type="radio" checked={level === item} onChange={() => changeFilter("level", item)} />
                    {item}
                  </label>
                );
              })}
            </div>

            <div className="filter-group">
              <h4>Price</h4>
              <label className="radio-row">
                <input type="radio" checked={price === ""} onChange={() => changeFilter("price", "")} />
                Any price
              </label>
              <label className="radio-row">
                <input type="radio" checked={price === "free"} onChange={() => changeFilter("price", "free")} />
                Free
              </label>
              <label className="radio-row">
                <input type="radio" checked={price === "paid"} onChange={() => changeFilter("price", "paid")} />
                Paid
              </label>
            </div>

            {hasFilters && (
              <button className="btn btn-ghost btn-block" onClick={() => setSearchParams({})}>
                <X size={16} /> Clear all filters
              </button>
            )}
          </aside>

          {/* Courses on the right */}
          <section>
            {loading && <CourseGridSkeleton count={6} />}

            {!loading && error && (
              <EmptyState icon={SearchX} title="Could not load courses" text="Please check your connection and try again." />
            )}

            {!loading && !error && result.items.length === 0 && (
              <EmptyState icon={SearchX} title="No courses found" text="Try other words or remove some filters.">
                {hasFilters && (
                  <button className="btn btn-primary" onClick={() => setSearchParams({})}>
                    Clear filters
                  </button>
                )}
              </EmptyState>
            )}

            {!loading && !error && (
              <div className="course-grid course-grid-3">
                {result.items.map(function (course, index) {
                  return <CourseCard key={course.id} course={course} index={index} />;
                })}
              </div>
            )}

            {!loading && totalPages > 1 && (
              <div className="pagination">
                <button className="btn btn-outline btn-small" disabled={page <= 1} onClick={() => goToPage(page - 1)}>
                  <ChevronLeft size={16} /> Previous
                </button>
                <span>
                  Page <strong>{page}</strong> of {totalPages}
                </span>
                <button
                  className="btn btn-outline btn-small"
                  disabled={page >= totalPages}
                  onClick={() => goToPage(page + 1)}
                >
                  Next <ChevronRight size={16} />
                </button>
              </div>
            )}
          </section>
        </div>
      </div>
    </div>
  );
}

export default CoursesPage;
