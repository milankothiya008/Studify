import { useEffect, useState } from "react";
import { Check, ChevronDown, ClipboardCheck, Pencil, Plus, Trash2 } from "lucide-react";
import api, { getErrorMessage } from "../../api";
import { useToast } from "../../ToastContext";

const LETTERS = ["A", "B", "C", "D"];
const EMPTY_QUESTION = { text: "", option1: "", option2: "", option3: "", option4: "", correctOption: 0, explanation: "" };

// Form for one quiz question: the question, 4 answers, which one is right, and an explanation.
function QuestionForm({ initial, onSave, onCancel }) {
  const [form, setForm] = useState(initial);
  const [saving, setSaving] = useState(false);

  function setField(name, value) {
    const copy = { ...form };
    copy[name] = value;
    setForm(copy);
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setSaving(true);
    await onSave(form);
    setSaving(false);
  }

  return (
    <form className="question-form" onSubmit={handleSubmit}>
      <div className="form-field">
        <label>Question</label>
        <textarea rows={2} value={form.text} onChange={(e) => setField("text", e.target.value)} maxLength={1000} required />
      </div>

      <label>Answers (click the circle of the correct one)</label>
      {[1, 2, 3, 4].map(function (number) {
        const fieldName = "option" + number;
        const isCorrect = form.correctOption === number;
        return (
          <div key={number} className={isCorrect ? "option-row correct" : "option-row"}>
            <button
              type="button"
              className="option-pick"
              onClick={() => setField("correctOption", number)}
              title="This is the correct answer"
            >
              {isCorrect ? <Check size={14} /> : LETTERS[number - 1]}
            </button>
            <input
              value={form[fieldName]}
              placeholder={"Answer " + LETTERS[number - 1]}
              onChange={(e) => setField(fieldName, e.target.value)}
              maxLength={500}
              required
            />
          </div>
        );
      })}

      <div className="form-field">
        <label>Explanation (shown after answering, optional)</label>
        <input value={form.explanation || ""} onChange={(e) => setField("explanation", e.target.value)} maxLength={1000} />
      </div>

      <div className="card-actions">
        <button type="submit" className="btn btn-primary btn-small" disabled={saving || form.correctOption === 0}>
          {form.correctOption === 0 ? "Choose the correct answer" : "Save question"}
        </button>
        <button type="button" className="btn btn-ghost btn-small" onClick={onCancel}>
          Cancel
        </button>
      </div>
    </form>
  );
}

// The quiz of one section in the curriculum editor.
function QuizEditor({ section, onChanged }) {
  const showToast = useToast();
  const [open, setOpen] = useState(false);
  const [quiz, setQuiz] = useState(null); // full quiz with answers, loaded when opened
  const [creating, setCreating] = useState(false);
  const [title, setTitle] = useState("Section " + section.title + " quiz");
  const [passPercent, setPassPercent] = useState(70);
  const [editingQuestionId, setEditingQuestionId] = useState(null); // "new" or a question id

  function loadQuiz() {
    api
      .get("/instructor/quizzes/" + section.quiz.id)
      .then(function (response) {
        setQuiz(response.data);
        setTitle(response.data.title);
        setPassPercent(response.data.passPercent);
      })
      .catch(function (err) {
        showToast(getErrorMessage(err), "error");
      });
  }

  useEffect(
    function () {
      if (open && section.quiz) {
        loadQuiz();
      }
    },
    [open]
  );

  async function createQuiz(event) {
    event.preventDefault();
    try {
      await api.post("/instructor/sections/" + section.id + "/quiz", { title: title, passPercent: Number(passPercent) });
      showToast("Quiz created. Now add some questions.");
      setCreating(false);
      setOpen(true);
      onChanged();
    } catch (err) {
      showToast(getErrorMessage(err), "error");
    }
  }

  async function saveSettings(event) {
    event.preventDefault();
    try {
      await api.put("/instructor/quizzes/" + quiz.id, { title: title, passPercent: Number(passPercent) });
      showToast("Quiz saved.");
      onChanged();
    } catch (err) {
      showToast(getErrorMessage(err), "error");
    }
  }

  async function deleteQuiz() {
    if (!window.confirm("Delete this quiz, its questions and all student attempts?")) {
      return;
    }
    try {
      await api.delete("/instructor/quizzes/" + section.quiz.id);
      showToast("Quiz deleted.");
      setOpen(false);
      setQuiz(null);
      onChanged();
    } catch (err) {
      showToast(getErrorMessage(err), "error");
    }
  }

  async function saveQuestion(form) {
    const body = { ...form, correctOption: Number(form.correctOption) };
    try {
      if (editingQuestionId === "new") {
        await api.post("/instructor/quizzes/" + quiz.id + "/questions", body);
        showToast("Question added.");
      } else {
        await api.put("/instructor/quiz-questions/" + editingQuestionId, body);
        showToast("Question saved.");
      }
      setEditingQuestionId(null);
      loadQuiz();
      onChanged();
    } catch (err) {
      showToast(getErrorMessage(err), "error");
    }
  }

  async function deleteQuestion(questionId) {
    if (!window.confirm("Delete this question?")) {
      return;
    }
    try {
      await api.delete("/instructor/quiz-questions/" + questionId);
      loadQuiz();
      onChanged();
    } catch (err) {
      showToast(getErrorMessage(err), "error");
    }
  }

  // ---------- No quiz yet ----------
  if (!section.quiz) {
    if (!creating) {
      return (
        <button className="add-quiz-button" onClick={() => setCreating(true)}>
          <ClipboardCheck size={16} /> Add a quiz to this section
        </button>
      );
    }
    return (
      <form className="quiz-editor add-row" onSubmit={createQuiz}>
        <input value={title} onChange={(e) => setTitle(e.target.value)} placeholder="Quiz title" maxLength={200} required />
        <label className="pass-field">
          Pass at
          <input type="number" min="1" max="100" value={passPercent} onChange={(e) => setPassPercent(e.target.value)} />%
        </label>
        <button type="submit" className="btn btn-primary btn-small">
          Create quiz
        </button>
        <button type="button" className="btn btn-ghost btn-small" onClick={() => setCreating(false)}>
          Cancel
        </button>
      </form>
    );
  }

  // ---------- The section has a quiz ----------
  return (
    <div className={open ? "quiz-editor open" : "quiz-editor"}>
      <button className="quiz-editor-header" onClick={() => setOpen(!open)}>
        <span>
          <ClipboardCheck size={18} /> <strong>Quiz: {section.quiz.title}</strong>
          <span className="muted small">
            {" "}
            · {section.quiz.questionCount} questions · pass at {section.quiz.passPercent}%
          </span>
        </span>
        <ChevronDown size={18} className="accordion-chevron" />
      </button>

      {open && quiz && (
        <div className="quiz-editor-body">
          <form className="add-row" onSubmit={saveSettings}>
            <input value={title} onChange={(e) => setTitle(e.target.value)} maxLength={200} required />
            <label className="pass-field">
              Pass at
              <input type="number" min="1" max="100" value={passPercent} onChange={(e) => setPassPercent(e.target.value)} />%
            </label>
            <button type="submit" className="btn btn-outline btn-small">
              Save
            </button>
            <button type="button" className="btn btn-ghost btn-small danger-text" onClick={deleteQuiz}>
              <Trash2 size={15} /> Delete quiz
            </button>
          </form>

          {quiz.attemptCount > 0 && (
            <p className="muted small">Students have taken this quiz {quiz.attemptCount} times.</p>
          )}

          {quiz.questions.map(function (question, index) {
            if (editingQuestionId === question.id) {
              return (
                <QuestionForm
                  key={question.id}
                  initial={question}
                  onSave={saveQuestion}
                  onCancel={() => setEditingQuestionId(null)}
                />
              );
            }
            const options = [question.option1, question.option2, question.option3, question.option4];
            return (
              <div key={question.id} className="quiz-editor-question">
                <div className="lecture-editor-row">
                  <strong>
                    {index + 1}. {question.text}
                  </strong>
                  <div className="icon-buttons">
                    <button title="Edit" onClick={() => setEditingQuestionId(question.id)}>
                      <Pencil size={16} />
                    </button>
                    <button title="Delete" className="danger" onClick={() => deleteQuestion(question.id)}>
                      <Trash2 size={16} />
                    </button>
                  </div>
                </div>
                <ul className="quiz-editor-options">
                  {options.map(function (option, optionIndex) {
                    const isCorrect = question.correctOption === optionIndex + 1;
                    return (
                      <li key={optionIndex} className={isCorrect ? "correct" : ""}>
                        {LETTERS[optionIndex]}. {option} {isCorrect && <Check size={14} />}
                      </li>
                    );
                  })}
                </ul>
              </div>
            );
          })}

          {editingQuestionId === "new" ? (
            <QuestionForm initial={EMPTY_QUESTION} onSave={saveQuestion} onCancel={() => setEditingQuestionId(null)} />
          ) : (
            <button className="btn btn-outline btn-small" onClick={() => setEditingQuestionId("new")}>
              <Plus size={16} /> Add question
            </button>
          )}
        </div>
      )}
    </div>
  );
}

export default QuizEditor;
