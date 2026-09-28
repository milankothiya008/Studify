import { useState } from "react";
import { AlertCircle, FolderPlus, ListVideo } from "lucide-react";
import api, { getErrorMessage } from "../../api";
import { useToast } from "../../ToastContext";
import SectionEditor from "./SectionEditor";

// The "Curriculum" tab: a list of sections, and each section has lectures.
function CurriculumEditor({ course, onChanged }) {
  const showToast = useToast();
  const [newSectionTitle, setNewSectionTitle] = useState("");
  const [error, setError] = useState("");
  const [adding, setAdding] = useState(false);

  async function handleAddSection(event) {
    event.preventDefault();
    setError("");
    setAdding(true);
    try {
      await api.post("/instructor/courses/" + course.id + "/sections", { title: newSectionTitle });
      setNewSectionTitle("");
      showToast("Section added.");
      onChanged();
    } catch (err) {
      setError(getErrorMessage(err));
    }
    setAdding(false);
  }

  return (
    <div>
      <h2>Curriculum</h2>
      <p className="muted">
        Split your course into sections, add lectures to each section, then upload a video for every lecture. Mark a few
        lectures as <strong>free preview</strong> so students can try the course before buying.
      </p>

      {error && (
        <div className="alert alert-error">
          <AlertCircle size={18} /> {error}
        </div>
      )}

      {course.sections.length === 0 && (
        <div className="curriculum-empty">
          <ListVideo size={32} />
          <p>No sections yet. Add your first section below, e.g. "Introduction".</p>
        </div>
      )}

      {course.sections.map(function (section, index) {
        return (
          <SectionEditor
            key={section.id}
            section={section}
            number={index + 1}
            isFirst={index === 0}
            isLast={index === course.sections.length - 1}
            onChanged={onChanged}
          />
        );
      })}

      <form className="add-row add-section" onSubmit={handleAddSection}>
        <input
          placeholder="New section title, e.g. Introduction"
          value={newSectionTitle}
          maxLength={200}
          onChange={(e) => setNewSectionTitle(e.target.value)}
          required
        />
        <button type="submit" className="btn btn-dark" disabled={adding}>
          <FolderPlus size={18} /> Add section
        </button>
      </form>
    </div>
  );
}

export default CurriculumEditor;
