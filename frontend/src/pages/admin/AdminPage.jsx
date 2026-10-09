import { useEffect, useState } from "react";
import { BookOpen, CreditCard, FolderTree, GraduationCap, IndianRupee, Sparkles, Tag, Users } from "lucide-react";
import api, { getErrorMessage } from "../../api";
import { useToast } from "../../ToastContext";
import PageHeader from "../../components/PageHeader";
import { SkeletonBlock } from "../../components/Skeleton";
import { formatMoney } from "../../utils";
import AdminUsers from "./AdminUsers";
import AdminCourses from "./AdminCourses";
import AdminCategories from "./AdminCategories";
import AdminPlans from "./AdminPlans";
import AdminCoupons from "./AdminCoupons";

// 1 -> "1 student", 3 -> "3 students"
function plural(count, word) {
  return count + " " + word + (count === 1 ? "" : "s");
}

const TABS = [
  { key: "users", label: "Users", icon: Users },
  { key: "courses", label: "Courses", icon: BookOpen },
  { key: "categories", label: "Categories", icon: FolderTree },
  { key: "plans", label: "Subscription plans", icon: CreditCard },
  { key: "coupons", label: "Coupons", icon: Tag },
];

function AdminPage() {
  const showToast = useToast();
  const [stats, setStats] = useState(null);
  const [activeTab, setActiveTab] = useState("users");
  const [addingDemo, setAddingDemo] = useState(false);
  const [addingActivity, setAddingActivity] = useState(false);
  const [refreshKey, setRefreshKey] = useState(0); // changing it reloads the open tab

  function loadStats() {
    api.get("/admin/stats").then(function (response) {
      setStats(response.data);
    });
  }

  useEffect(function () {
    loadStats();
  }, []);

  // Adds the demo courses from backend/Data/DemoCourses.cs (only the ones that are missing).
  async function handleAddDemoCourses() {
    setAddingDemo(true);
    try {
      const response = await api.post("/admin/demo-courses");
      showToast(response.data.message, response.data.added > 0 ? "success" : "info");
      loadStats();
      setRefreshKey(refreshKey + 1);
    } catch (err) {
      showToast(getErrorMessage(err), "error");
    }
    setAddingDemo(false);
  }

  // Adds demo students and activity from backend/Data/DemoActivity.cs (runs only once).
  async function handleAddDemoActivity() {
    if (!window.confirm("Add demo students, purchases, reviews, Q&A and more? This runs only once.")) {
      return;
    }
    setAddingActivity(true);
    try {
      const response = await api.post("/admin/demo-activity");
      showToast(response.data.message);
      loadStats();
      setRefreshKey(refreshKey + 1);
    } catch (err) {
      showToast(getErrorMessage(err), "error");
    }
    setAddingActivity(false);
  }

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
      <PageHeader title="Admin panel" subtitle="Platform numbers, users, courses, categories and plans.">
        <button className="btn btn-white" onClick={handleAddDemoCourses} disabled={addingDemo}>
          {addingDemo ? <span className="btn-spinner btn-spinner-dark"></span> : <Sparkles size={18} />}
          Add demo courses
        </button>
        <button className="btn btn-glass" onClick={handleAddDemoActivity} disabled={addingActivity}>
          {addingActivity ? <span className="btn-spinner"></span> : <Users size={18} />}
          Add demo activity
        </button>
      </PageHeader>

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

        <div className="tab-content" key={activeTab + "-" + refreshKey}>
          {activeTab === "users" && <AdminUsers />}
          {activeTab === "courses" && <AdminCourses />}
          {activeTab === "categories" && <AdminCategories />}
          {activeTab === "plans" && <AdminPlans />}
          {activeTab === "coupons" && <AdminCoupons />}
        </div>
      </div>
    </div>
  );
}

export default AdminPage;
