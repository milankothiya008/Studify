import { Routes, Route, useLocation } from "react-router-dom";
import Navbar from "./components/Navbar";
import Footer from "./components/Footer";
import ProtectedRoute from "./components/ProtectedRoute";

import HomePage from "./pages/HomePage";
import CoursesPage from "./pages/CoursesPage";
import CourseDetailPage from "./pages/CourseDetailPage";
import LoginPage from "./pages/LoginPage";
import RegisterPage from "./pages/RegisterPage";
import MyLearningPage from "./pages/MyLearningPage";
import CoursePlayerPage from "./pages/CoursePlayerPage";
import PlansPage from "./pages/PlansPage";
import CheckoutPage from "./pages/CheckoutPage";
import PurchaseHistoryPage from "./pages/PurchaseHistoryPage";
import ProfilePage from "./pages/ProfilePage";
import CertificatePage from "./pages/CertificatePage";
import NotFoundPage from "./pages/NotFoundPage";
import InstructorDashboardPage from "./pages/instructor/InstructorDashboardPage";
import CourseEditorPage from "./pages/instructor/CourseEditorPage";
import AdminPage from "./pages/admin/AdminPage";

function App() {
  const location = useLocation();

  // The course player uses the whole screen, so it has no footer.
  const isPlayerPage = location.pathname.startsWith("/learn/");

  return (
    <div className="app">
      <Navbar />

      <main className="main-content">
        <Routes>
          {/* Pages for everyone */}
          <Route path="/" element={<HomePage />} />
          <Route path="/courses" element={<CoursesPage />} />
          <Route path="/course/:id" element={<CourseDetailPage />} />
          <Route path="/plans" element={<PlansPage />} />
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />

          {/* Pages for logged in users */}
          <Route path="/my-learning" element={<ProtectedRoute><MyLearningPage /></ProtectedRoute>} />
          <Route path="/learn/:courseId" element={<ProtectedRoute><CoursePlayerPage /></ProtectedRoute>} />
          <Route path="/checkout/:type/:id" element={<ProtectedRoute><CheckoutPage /></ProtectedRoute>} />
          <Route path="/purchases" element={<ProtectedRoute><PurchaseHistoryPage /></ProtectedRoute>} />
          <Route path="/profile" element={<ProtectedRoute><ProfilePage /></ProtectedRoute>} />
          <Route path="/certificate/:courseId" element={<ProtectedRoute><CertificatePage /></ProtectedRoute>} />

          {/* Pages for instructors */}
          <Route
            path="/instructor"
            element={<ProtectedRoute roles={["Instructor", "Admin"]}><InstructorDashboardPage /></ProtectedRoute>}
          />
          <Route
            path="/instructor/course/:id"
            element={<ProtectedRoute roles={["Instructor", "Admin"]}><CourseEditorPage /></ProtectedRoute>}
          />

          {/* Page for admins */}
          <Route path="/admin" element={<ProtectedRoute roles={["Admin"]}><AdminPage /></ProtectedRoute>} />

          <Route path="*" element={<NotFoundPage />} />
        </Routes>
      </main>

      {!isPlayerPage && <Footer />}
    </div>
  );
}

export default App;
