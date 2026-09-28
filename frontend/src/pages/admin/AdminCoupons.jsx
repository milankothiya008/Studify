import { useEffect, useState } from "react";
import api from "../../api";
import CouponManager from "../../components/CouponManager";

// Admins: coupons for all courses, subscription plans, or one course.
function AdminCoupons() {
  const [courses, setCourses] = useState([]);

  useEffect(function () {
    api.get("/admin/courses").then(function (response) {
      setCourses(response.data);
    });
  }, []);

  return <CouponManager isAdmin courses={courses} />;
}

export default AdminCoupons;
