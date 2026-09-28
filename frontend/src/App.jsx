import { useEffect } from "react";
import { Routes, Route, useLocation } from "react-router-dom";
import Navbar from "./components/Navbar";
import Footer from "./components/Footer";
import ProtectedRoute from "./components/ProtectedRoute";

import HomePage from "./pages/HomePage";
import CoursesPage from "./pages/CoursesPage";
import CourseDetailPage from "./pages/CourseDetailPage";
import LoginPage from "./pages/LoginPage";
import RegisterPage from "./pages/RegisterPage";
import VerifyEmailPage from "./pages/VerifyEmailPage";
import ForgotPasswordPage from "./pages/ForgotPasswordPage";
import MyLearningPage from "./pages/MyLearningPage";
import CoursePlayerPage from "./pages/CoursePlayerPage";
import PlansPage from "./pages/PlansPage";
import CheckoutPage from "./pages/CheckoutPage";
import PurchaseHistoryPage from "./pages/PurchaseHistoryPage";
import ProfilePage from "./pages/ProfilePage";
import CertificatePage from "./pages/CertificatePage";
import NotFoundPage from "./pages/NotFoundPage";
import WishlistPage from "./pages/WishlistPage";
import VerifyCertificatePage from "./pages/VerifyCertificatePage";
import InstructorQuestionsPage from "./pages/instructor/InstructorQuestionsPage";
import InstructorDashboardPage from "./pages/instructor/InstructorDashboardPage";
import CourseEditorPage from "./pages/instructor/CourseEditorPage";
import AdminPage from "./pages/admin/AdminPage";

// Pages that use the whole screen and have no footer.
const FULL_SCREEN_PAGES = ["/learn/", "/login", "/register", "/verify-email", "/forgot-password"];

function App() {
  const location = useLocation();

  // Start every new page at the top.
  useEffect(
    function () {
      window.scrollTo(0, 0);
    },
    [location.pathname]
  );

  const isFullScreen = FULL_SCREEN_PAGES.some((path) => location.pathname.startsWith(path));

  return (
    <div className="app">
      <Navbar />

      {/* key = the address, so each new page plays the fade-in animation */}
      <main className="main-content page-transition" key={location.pathname}>
        <Routes>
          {/* Pages for everyone */}
          <Route path="/" element={<HomePage />} />
          <Route path="/courses" element={<CoursesPage />} />
          <Route path="/course/:id" element={<CourseDetailPage />} />
          <Route path="/plans" element={<PlansPage />} />
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/verify-email" element={<VerifyEmailPage />} />
          <Route path="/forgot-password" element={<ForgotPasswordPage />} />
          <Route path="/verify" element={<VerifyCertificatePage />} />
          <Route path="/verify/:number" element={<VerifyCertificatePage />} />

          {/* Pages for logged in users */}
          <Route path="/my-learning" element={<ProtectedRoute><MyLearningPage /></ProtectedRoute>} />
          <Route path="/learn/:courseId" element={<ProtectedRoute><CoursePlayerPage /></ProtectedRoute>} />
          <Route path="/checkout/:type/:id" element={<ProtectedRoute><CheckoutPage /></ProtectedRoute>} />
          <Route path="/purchases" element={<ProtectedRoute><PurchaseHistoryPage /></ProtectedRoute>} />
          <Route path="/profile" element={<ProtectedRoute><ProfilePage /></ProtectedRoute>} />
          <Route path="/certificate/:courseId" element={<ProtectedRoute><CertificatePage /></ProtectedRoute>} />
          <Route path="/wishlist" element={<ProtectedRoute><WishlistPage /></ProtectedRoute>} />

          {/* Pages for instructors */}
          <Route
            path="/instructor"
            element={<ProtectedRoute roles={["Instructor", "Admin"]}><InstructorDashboardPage /></ProtectedRoute>}
          />
          <Route
            path="/instructor/questions"
            element={<ProtectedRoute roles={["Instructor", "Admin"]}><InstructorQuestionsPage /></ProtectedRoute>}
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

      {!isFullScreen && <Footer />}
    </div>
  );
}

export default App;
