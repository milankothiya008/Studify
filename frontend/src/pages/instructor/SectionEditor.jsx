import { useState } from "react";
import { AlertCircle, ArrowDown, ArrowUp, Pencil, Plus, Trash2 } from "lucide-react";
import api, { getErrorMessage } from "../../api";
import { useToast } from "../../ToastContext";
import LectureEditor from "./LectureEditor";

// One section in the curriculum editor, with its lectures.
function SectionEditor({ section, number, isFirst, isLast, onChanged }) {
  const showToast = useToast();
  const [isEditing, setIsEditing] = useState(false);
  const [title, setTitle] = useState(section.title);
  const [newLectureTitle, setNewLectureTitle] = useState("");
  const [newLectureIsPreview, setNewLectureIsPreview] = useState(false);
  const [error, setError] = useState("");

  async function handleRename(event) {
    event.preventDefault();
    setError("");
    try {
      await api.put("/instructor/sections/" + section.id, { title: title });
      setIsEditing(false);
      showToast("Section renamed.");
      onChanged();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  }

  async function handleMove(direction) {
    try {
      await api.post("/instructor/sections/" + section.id + "/move?direction=" + direction);
      onChanged();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  }

  async function handleDelete() {
    if (!window.confirm('Delete section "' + section.title + '" and all its lectures?')) {
      return;
    }
    try {
      await api.delete("/instructor/sections/" + section.id);
      showToast("Section deleted.");
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
      showToast("Lecture added. Now upload its video.");
      onChanged();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  }

  return (
    <div className="section-editor">
      <div className="section-editor-header">
        {isEditing ? (
          <form className="add-row" onSubmit={handleRename}>
            <input value={title} onChange={(e) => setTitle(e.target.value)} maxLength={200} autoFocus required />
            <button type="submit" className="btn btn-primary btn-small">
              Save
            </button>
            <button
              type="button"
              className="btn btn-ghost btn-small"
              onClick={() => {
                setTitle(section.title);
                setIsEditing(false);
              }}
            >
              Cancel
            </button>
          </form>
        ) : (
          <>
            <div className="section-editor-title">
              <span className="section-number">Section {number}</span>
              <strong>{section.title}</strong>
            </div>
            <div className="icon-buttons">
              <button title="Move up" disabled={isFirst} onClick={() => handleMove("up")}>
                <ArrowUp size={16} />
              </button>
              <button title="Move down" disabled={isLast} onClick={() => handleMove("down")}>
                <ArrowDown size={16} />
              </button>
              <button title="Rename" onClick={() => setIsEditing(true)}>
                <Pencil size={16} />
              </button>
              <button title="Delete" className="danger" onClick={handleDelete}>
                <Trash2 size={16} />
              </button>
            </div>
          </>
        )}
      </div>

      {error && (
        <div className="alert alert-error">
          <AlertCircle size={18} /> {error}
        </div>
      )}

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

      <form className="add-row add-lecture" onSubmit={handleAddLecture}>
        <input
          placeholder="New lecture title"
          value={newLectureTitle}
          maxLength={200}
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
          <Plus size={16} /> Add lecture
        </button>
      </form>
    </div>
  );
}

export default SectionEditor;
