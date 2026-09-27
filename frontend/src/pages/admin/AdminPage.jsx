import { useEffect, useState } from "react";
import api from "../../api";
import { formatMoney } from "../../utils";
import AdminUsers from "./AdminUsers";
import AdminCourses from "./AdminCourses";
import AdminCategories from "./AdminCategories";
import AdminPlans from "./AdminPlans";

function AdminPage() {
  const [stats, setStats] = useState(null);
  const [activeTab, setActiveTab] = useState("users");

  useEffect(function () {
    api.get("/admin/stats").then(function (response) {
      setStats(response.data);
    });
  }, []);

  const tabs = [
    { key: "users", label: "Users" },
    { key: "courses", label: "Courses" },
    { key: "categories", label: "Categories" },
    { key: "plans", label: "Subscription plans" },
  ];

  return (
    <div className="container page">
      <h1>Admin panel</h1>

      {stats && (
        <div className="stat-grid">
          <div className="stat">
            <span>Revenue</span>
            <strong>{formatMoney(stats.totalRevenue)}</strong>
          </div>
          <div className="stat">
            <span>Users</span>
            <strong>{stats.totalUsers}</strong>
            <small>
              {stats.totalStudents} students · {stats.totalInstructors} instructors
            </small>
          </div>
          <div className="stat">
            <span>Courses</span>
            <strong>{stats.totalCourses}</strong>
            <small>{stats.publishedCourses} published</small>
          </div>
          <div className="stat">
            <span>Enrollments</span>
            <strong>{stats.totalEnrollments}</strong>
            <small>{stats.activeSubscriptions} active subscribers</small>
          </div>
        </div>
      )}

      <div className="tabs">
        {tabs.map(function (tab) {
          return (
            <button
              key={tab.key}
              className={activeTab === tab.key ? "tab active" : "tab"}
              onClick={() => setActiveTab(tab.key)}
            >
              {tab.label}
            </button>
          );
        })}
      </div>

      <div className="tab-content">
        {activeTab === "users" && <AdminUsers />}
        {activeTab === "courses" && <AdminCourses />}
        {activeTab === "categories" && <AdminCategories />}
        {activeTab === "plans" && <AdminPlans />}
      </div>
    </div>
  );
}

export default AdminPage;
