import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { ArrowLeft, Check, Eye, FileText, Image, ListVideo, Rocket, ShieldAlert } from "lucide-react";
import api, { getErrorMessage } from "../../api";
import { useAuth } from "../../AuthContext";
import Spinner from "../../components/Spinner";
import EmptyState from "../../components/EmptyState";
import ProgressBar from "../../components/ProgressBar";
import CourseDetailsForm from "./CourseDetailsForm";
import CourseMediaForm from "./CourseMediaForm";
import CurriculumEditor from "./CurriculumEditor";
import PublishPanel from "./PublishPanel";

// Course editor with 4 steps on the left: details, media, curriculum and publish.
function CourseEditorPage() {
  const { id } = useParams();
  const { user } = useAuth();
  const [course, setCourse] = useState(null);
  const [categories, setCategories] = useState([]);
  const [activeTab, setActiveTab] = useState("details");
  const [error, setError] = useState("");

  // Loads the course again. The child components call this after every change.
  async function loadCourse() {
    try {
      const response = await api.get("/courses/" + id);
      setCourse(response.data);
    } catch (err) {
      setError(getErrorMessage(err));
    }
  }

  useEffect(
    function () {
      loadCourse();
      api.get("/categories").then(function (response) {
        setCategories(response.data);
      });
    },
    [id]
  );

  if (error) {
    return (
      <div className="container page">
        <EmptyState icon={ShieldAlert} title={error}>
          <Link to="/instructor" className="btn btn-primary">
            Back to dashboard
          </Link>
        </EmptyState>
      </div>
    );
  }

  if (!course) {
    return <Spinner />;
  }

  // Instructors can edit only their own courses. Admins can edit any course.
  if (!course.isOwner && user.role !== "Admin") {
    return (
      <div className="container page">
        <EmptyState icon={ShieldAlert} title="You can only edit your own courses." />
      </div>
    );
  }

  // Ticks shown next to each step.
  const detailsDone = Boolean(course.subtitle && course.description && course.categoryId);
  const mediaDone = Boolean(course.thumbnailUrl);
  let lecturesWithVideo = 0;
  course.sections.forEach(function (section) {
    section.lectures.forEach(function (lecture) {
      if (lecture.hasVideo) {
        lecturesWithVideo++;
      }
    });
  });
  const curriculumDone = lecturesWithVideo > 0;

  const tabs = [
    { key: "details", label: "Course details", icon: FileText, done: detailsDone },
    { key: "media", label: "Image & promo video", icon: Image, done: mediaDone },
    { key: "curriculum", label: "Curriculum", icon: ListVideo, done: curriculumDone },
    { key: "publish", label: "Publish", icon: Rocket, done: course.isPublished },
  ];
  const doneCount = tabs.filter((tab) => tab.done).length;

  return (
    <div>
      <div className="editor-topbar">
        <div className="container editor-topbar-inner">
          <Link to="/instructor" className="back-link back-link-light">
            <ArrowLeft size={18} /> Back
          </Link>
          <strong className="editor-title">{course.title}</strong>
          {course.isPublished ? (
            <span className="badge badge-success">Published</span>
          ) : (
            <span className="badge badge-warning">Draft</span>
          )}
          <Link to={"/course/" + course.id} className="btn btn-outline-light btn-small">
            <Eye size={16} /> Preview
          </Link>
        </div>
      </div>

      <div className="container editor-layout">
        <nav className="editor-steps card">
          <div className="editor-steps-progress">
            <small>
              {doneCount} of {tabs.length} steps done
            </small>
            <ProgressBar percent={(doneCount * 100) / tabs.length} />
          </div>
          {tabs.map(function (tab) {
            const Icon = tab.icon;
            return (
              <button
                key={tab.key}
                className={activeTab === tab.key ? "editor-step active" : "editor-step"}
                onClick={() => setActiveTab(tab.key)}
              >
                <span className={tab.done ? "step-check done" : "step-check"}>
                  {tab.done ? <Check size={14} /> : <Icon size={14} />}
                </span>
                {tab.label}
              </button>
            );
          })}
        </nav>

        <section className="editor-content card" key={activeTab}>
          {activeTab === "details" && (
            <CourseDetailsForm course={course} categories={categories} onSaved={loadCourse} />
          )}
          {activeTab === "media" && <CourseMediaForm course={course} onChanged={loadCourse} />}
          {activeTab === "curriculum" && <CurriculumEditor course={course} onChanged={loadCourse} />}
          {activeTab === "publish" && <PublishPanel course={course} onChanged={loadCourse} />}
        </section>
      </div>
    </div>
  );
}

export default CourseEditorPage;
