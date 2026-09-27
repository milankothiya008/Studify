import { useState } from "react";
import api, { getErrorMessage } from "../../api";
import SectionEditor from "./SectionEditor";

// The "Curriculum" tab: a list of sections, and each section has lectures.
function CurriculumEditor({ course, onChanged }) {
  const [newSectionTitle, setNewSectionTitle] = useState("");
  const [error, setError] = useState("");

  async function handleAddSection(event) {
    event.preventDefault();
    setError("");
    try {
      await api.post("/instructor/courses/" + course.id + "/sections", { title: newSectionTitle });
      setNewSectionTitle("");
      onChanged();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  }

  return (
    <div>
      <h2>Curriculum</h2>
      <p className="muted">
        Split your course into sections, add lectures to each section, then upload a video for every lecture. Mark a
        few lectures as <strong>free preview</strong> so students can try the course before buying.
      </p>

      {error && <div className="alert alert-error">{error}</div>}

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

      <form className="add-section" onSubmit={handleAddSection}>
        <input
          placeholder="New section title, e.g. Introduction"
          value={newSectionTitle}
          onChange={(e) => setNewSectionTitle(e.target.value)}
          required
        />
        <button type="submit" className="btn btn-dark">
          + Add section
        </button>
      </form>
    </div>
  );
}

export default CurriculumEditor;
