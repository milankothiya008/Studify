import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Eye, Pencil } from "lucide-react";
import api from "../../api";
import { SkeletonRows } from "../../components/Skeleton";
import { formatDate, formatPrice } from "../../utils";

// Every course on the platform, including drafts.
function AdminCourses() {
  const [courses, setCourses] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(function () {
    api
      .get("/admin/courses")
      .then(function (response) {
        setCourses(response.data);
      })
      .finally(function () {
        setLoading(false);
      });
  }, []);

  if (loading) {
    return <SkeletonRows rows={5} />;
  }

  return (
    <div className="card table-card">
      <div className="table-wrapper">
        <table className="table">
          <thead>
            <tr>
              <th>Course</th>
              <th>Instructor</th>
              <th>Status</th>
              <th>Price</th>
              <th>Students</th>
              <th>Updated</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {courses.map(function (course) {
              return (
                <tr key={course.id}>
                  <td>
                    <strong>{course.title}</strong>
                  </td>
                  <td>{course.instructorName}</td>
                  <td>
                    {course.isPublished ? (
                      <span className="badge badge-success">Published</span>
                    ) : (
                      <span className="badge badge-warning">Draft</span>
                    )}
                  </td>
                  <td>{formatPrice(course.price)}</td>
                  <td>{course.studentCount}</td>
                  <td>{formatDate(course.updatedAt)}</td>
                  <td>
                    <div className="table-actions">
                      <Link to={"/course/" + course.id} className="icon-button" title="View">
                        <Eye size={16} />
                      </Link>
                      <Link to={"/instructor/course/" + course.id} className="icon-button" title="Edit">
                        <Pencil size={16} />
                      </Link>
                    </div>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}

export default AdminCourses;
