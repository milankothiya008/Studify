import { useEffect, useState } from "react";
import { BookOpen, CreditCard, FolderTree, GraduationCap, IndianRupee, Users } from "lucide-react";
import api from "../../api";
import PageHeader from "../../components/PageHeader";
import { SkeletonBlock } from "../../components/Skeleton";
import { formatMoney } from "../../utils";
import AdminUsers from "./AdminUsers";
import AdminCourses from "./AdminCourses";
import AdminCategories from "./AdminCategories";
import AdminPlans from "./AdminPlans";

// 1 -> "1 student", 3 -> "3 students"
function plural(count, word) {
  return count + " " + word + (count === 1 ? "" : "s");
}

const TABS = [
  { key: "users", label: "Users", icon: Users },
  { key: "courses", label: "Courses", icon: BookOpen },
  { key: "categories", label: "Categories", icon: FolderTree },
  { key: "plans", label: "Subscription plans", icon: CreditCard },
];

function AdminPage() {
  const [stats, setStats] = useState(null);
  const [activeTab, setActiveTab] = useState("users");

  useEffect(function () {
    api.get("/admin/stats").then(function (response) {
      setStats(response.data);
    });
  }, []);

  const statCards = stats
    ? [
        { icon: IndianRupee, label: "Revenue", value: formatMoney(stats.totalRevenue), note: "all payments" },
        {
          icon: Users,
          label: "Users",
          value: stats.totalUsers,
          note: plural(stats.totalStudents, "student") + " · " + plural(stats.totalInstructors, "instructor"),
        },
        { icon: BookOpen, label: "Courses", value: stats.totalCourses, note: stats.publishedCourses + " published" },
        {
          icon: GraduationCap,
          label: "Enrollments",
          value: stats.totalEnrollments,
          note: plural(stats.activeSubscriptions, "active subscriber"),
        },
      ]
    : [];

  return (
    <div>
      <PageHeader title="Admin panel" subtitle="Platform numbers, users, courses, categories and plans." />

      <div className="container page">
        <div className="stat-grid">
          {!stats && (
            <>
              <SkeletonBlock height={96} />
              <SkeletonBlock height={96} />
              <SkeletonBlock height={96} />
              <SkeletonBlock height={96} />
            </>
          )}
          {statCards.map(function (stat, index) {
            const Icon = stat.icon;
            return (
              <div key={stat.label} className="stat card stagger-item" style={{ "--i": index }}>
                <span className="stat-icon">
                  <Icon size={22} />
                </span>
                <div>
                  <span className="stat-label">{stat.label}</span>
                  <strong className="stat-value">{stat.value}</strong>
                  <small className="stat-note">{stat.note}</small>
                </div>
              </div>
            );
          })}
        </div>

        <div className="tabs">
          {TABS.map(function (tab) {
            const Icon = tab.icon;
            return (
              <button
                key={tab.key}
                className={activeTab === tab.key ? "tab active" : "tab"}
                onClick={() => setActiveTab(tab.key)}
              >
                <Icon size={16} /> {tab.label}
              </button>
            );
          })}
        </div>

        <div className="tab-content" key={activeTab}>
          {activeTab === "users" && <AdminUsers />}
          {activeTab === "courses" && <AdminCourses />}
          {activeTab === "categories" && <AdminCategories />}
          {activeTab === "plans" && <AdminPlans />}
        </div>
      </div>
    </div>
  );
}

export default AdminPage;
