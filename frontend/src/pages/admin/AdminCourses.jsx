import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import api from "../../api";
import { formatDate, formatPrice } from "../../utils";

// Every course on the platform, including drafts.
function AdminCourses() {
  const [courses, setCourses] = useState([]);

  useEffect(function () {
    api.get("/admin/courses").then(function (response) {
      setCourses(response.data);
    });
  }, []);

  return (
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
                <td>{course.title}</td>
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
                  <Link to={"/course/" + course.id}>View</Link> ·{" "}
                  <Link to={"/instructor/course/" + course.id}>Edit</Link>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

export default AdminCourses;
