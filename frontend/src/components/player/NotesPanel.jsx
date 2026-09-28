import { useEffect, useState } from "react";
import { NotebookPen, Play, Plus, Trash2 } from "lucide-react";
import api, { getErrorMessage } from "../../api";
import { useToast } from "../../ToastContext";
import { formatClock } from "../../utils";

// The "Notes" tab of the course player.
// currentSecond = where the video is now; the new note is saved at that moment.
// onJump(lectureId, seconds) = open that lecture at that second.
function NotesPanel({ courseId, lecture, currentSecond, canSave, onJump }) {
  const showToast = useToast();
  const [notes, setNotes] = useState([]);
  const [text, setText] = useState("");
  const [saving, setSaving] = useState(false);

  function load() {
    api
      .get("/courses/" + courseId + "/notes")
      .then(function (response) {
        setNotes(response.data);
      })
      .catch(function () {
        setNotes([]);
      });
  }

  useEffect(
    function () {
      load();
    },
    [courseId]
  );

  async function handleAdd(event) {
    event.preventDefault();
    if (!lecture) {
      return;
    }
    setSaving(true);
    try {
      await api.post("/lectures/" + lecture.id + "/notes", { seconds: currentSecond, text: text });
      setText("");
      showToast("Note saved at " + formatClock(currentSecond) + ".");
      load();
    } catch (err) {
      showToast(getErrorMessage(err), "error");
    }
    setSaving(false);
  }

  async function handleDelete(noteId) {
    try {
      await api.delete("/notes/" + noteId);
      load();
    } catch (err) {
      showToast(getErrorMessage(err), "error");
    }
  }

  // Group the notes by lecture title, keeping the course order.
  const groups = [];
  notes.forEach(function (note) {
    let group = groups.find((g) => g.lectureId === note.lectureId);
    if (!group) {
      group = { lectureId: note.lectureId, lectureTitle: note.lectureTitle, notes: [] };
      groups.push(group);
    }
    group.notes.push(note);
  });

  return (
    <div className="notes-panel">
      {canSave && lecture && (
        <form className="note-form" onSubmit={handleAdd}>
          <span className="note-time">{formatClock(currentSecond)}</span>
          <textarea
            rows={2}
            placeholder="Write a note for this moment of the video..."
            value={text}
            onChange={(e) => setText(e.target.value)}
            maxLength={2000}
            required
          />
          <button type="submit" className="btn btn-primary btn-small" disabled={saving || !text.trim()}>
            <Plus size={16} /> Save note
          </button>
        </form>
      )}

      {!canSave && <p className="muted small">Enroll in the course to take notes.</p>}

      {notes.length === 0 && (
        <div className="qa-empty">
          <NotebookPen size={28} />
          <p>No notes yet. Pause at an important moment and write it down.</p>
        </div>
      )}

      {groups.map(function (group) {
        return (
          <div key={group.lectureId} className="note-group">
            <h4>{group.lectureTitle}</h4>
            {group.notes.map(function (note) {
              return (
                <div key={note.id} className="note">
                  <button className="note-jump" onClick={() => onJump(note.lectureId, note.seconds)} title="Play from here">
                    <Play size={12} /> {formatClock(note.seconds)}
                  </button>
                  <p className="pre-line">{note.text}</p>
                  <button className="icon-button danger" onClick={() => handleDelete(note.id)} aria-label="Delete note">
                    <Trash2 size={16} />
                  </button>
                </div>
              );
            })}
          </div>
        );
      })}
    </div>
  );
}

export default NotesPanel;
