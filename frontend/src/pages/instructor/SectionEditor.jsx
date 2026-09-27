import { useState } from "react";
import api, { getErrorMessage } from "../../api";
import LectureEditor from "./LectureEditor";

// One section in the curriculum editor, with its lectures.
function SectionEditor({ section, number, isFirst, isLast, onChanged }) {
  const [isEditing, setIsEditing] = useState(false);
  const [title, setTitle] = useState(section.title);
  const [newLectureTitle, setNewLectureTitle] = useState("");
  const [newLectureIsPreview, setNewLectureIsPreview] = useState(false);
  const [error, setError] = useState("");

  async function handleRename(event) {
    event.preventDefault();
    try {
      await api.put("/instructor/sections/" + section.id, { title: title });
      setIsEditing(false);
      onChanged();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  }

  async function handleMove(direction) {
    await api.post("/instructor/sections/" + section.id + "/move?direction=" + direction);
    onChanged();
  }

  async function handleDelete() {
    if (!window.confirm('Delete section "' + section.title + '" and all its lectures?')) {
      return;
    }
    try {
      await api.delete("/instructor/sections/" + section.id);
      onChanged();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  }

  async function handleAddLecture(event) {
    event.preventDefault();
    setError("");
    try {
      await api.post("/instructor/sections/" + section.id + "/lectures", {
        title: newLectureTitle,
        description: "",
        isFreePreview: newLectureIsPreview,
      });
      setNewLectureTitle("");
      setNewLectureIsPreview(false);
      onChanged();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  }

  return (
    <div className="section-editor">
      <div className="section-editor-header">
        {isEditing ? (
          <form className="inline-form" onSubmit={handleRename}>
            <input value={title} onChange={(e) => setTitle(e.target.value)} required />
            <button type="submit" className="btn btn-primary btn-small">
              Save
            </button>
            <button type="button" className="btn btn-outline btn-small" onClick={() => setIsEditing(false)}>
              Cancel
            </button>
          </form>
        ) : (
          <>
            <strong>
              Section {number}: {section.title}
            </strong>
            <div className="icon-buttons">
              <button title="Move up" disabled={isFirst} onClick={() => handleMove("up")}>
                ↑
              </button>
              <button title="Move down" disabled={isLast} onClick={() => handleMove("down")}>
                ↓
              </button>
              <button title="Rename" onClick={() => setIsEditing(true)}>
                ✎
              </button>
              <button title="Delete" onClick={handleDelete}>
                🗑
              </button>
            </div>
          </>
        )}
      </div>

      {error && <div className="alert alert-error">{error}</div>}

      {section.lectures.map(function (lecture, index) {
        return (
          <LectureEditor
            key={lecture.id}
            lecture={lecture}
            number={index + 1}
            isFirst={index === 0}
            isLast={index === section.lectures.length - 1}
            onChanged={onChanged}
          />
        );
      })}

      <form className="add-lecture" onSubmit={handleAddLecture}>
        <input
          placeholder="New lecture title"
          value={newLectureTitle}
          onChange={(e) => setNewLectureTitle(e.target.value)}
          required
        />
        <label className="checkbox-label">
          <input
            type="checkbox"
            checked={newLectureIsPreview}
            onChange={(e) => setNewLectureIsPreview(e.target.checked)}
          />
          Free preview
        </label>
        <button type="submit" className="btn btn-outline btn-small">
          + Add lecture
        </button>
      </form>
    </div>
  );
}

export default SectionEditor;
