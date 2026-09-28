import { useEffect, useState } from "react";
import { MessageCircleQuestion, PlayCircle, Plus, Search, X } from "lucide-react";
import api, { getErrorMessage } from "../../api";
import { useToast } from "../../ToastContext";
import Avatar from "../Avatar";
import QuestionThread from "../QuestionThread";
import { SkeletonRows } from "../Skeleton";
import { timeAgo } from "../../utils";

// The "Q&A" tab of the course player.
// Shows questions about the current lecture (or the whole course), lets students ask,
// and opens one question with its answers.
function QuestionsPanel({ courseId, lecture, openQuestionId, onOpenQuestion }) {
  const showToast = useToast();
  const [scope, setScope] = useState("lecture"); // "lecture" or "course"
  const [search, setSearch] = useState("");
  const [questions, setQuestions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [asking, setAsking] = useState(false);
  const [title, setTitle] = useState("");
  const [body, setBody] = useState("");
  const [posting, setPosting] = useState(false);

  const lectureId = lecture ? lecture.id : null;

  function load() {
    setLoading(true);
    const params = { search: search };
    if (scope === "lecture" && lectureId) {
      params.lectureId = lectureId;
    }
    api
      .get("/courses/" + courseId + "/questions", { params: params })
      .then(function (response) {
        setQuestions(response.data);
      })
      .catch(function () {
        setQuestions([]);
      })
      .finally(function () {
        setLoading(false);
      });
  }

  // Wait a moment after typing in the search box, then load.
  useEffect(
    function () {
      const timer = setTimeout(load, 300);
      return function () {
        clearTimeout(timer);
      };
    },
    [courseId, lectureId, scope, search]
  );

  async function handleAsk(event) {
    event.preventDefault();
    setPosting(true);
    try {
      await api.post("/courses/" + courseId + "/questions", {
        lectureId: lectureId,
        title: title,
        body: body,
      });
      showToast("Your question was posted. The instructor has been notified.");
      setTitle("");
      setBody("");
      setAsking(false);
      setScope("lecture");
      load();
    } catch (err) {
      showToast(getErrorMessage(err), "error");
    }
    setPosting(false);
  }

  // One question is open: show it with its answers.
  if (openQuestionId) {
    return <QuestionThread questionId={openQuestionId} onBack={() => onOpenQuestion(null)} onChanged={load} />;
  }

  return (
    <div className="qa-panel">
      <div className="qa-toolbar">
        <div className="input-icon">
          <Search size={18} />
          <input placeholder="Search questions" value={search} onChange={(e) => setSearch(e.target.value)} />
        </div>
        <select className="select-small" value={scope} onChange={(e) => setScope(e.target.value)}>
          <option value="lecture">This lecture</option>
          <option value="course">All lectures</option>
        </select>
        <button className="btn btn-primary btn-small" onClick={() => setAsking(!asking)}>
          {asking ? <X size={16} /> : <Plus size={16} />} {asking ? "Cancel" : "Ask a question"}
        </button>
      </div>

      {asking && (
        <form className="ask-form card card-padded" onSubmit={handleAsk}>
          <p className="muted small">
            Asking about: <strong>{lecture ? lecture.title : "the whole course"}</strong>
          </p>
          <div className="form-field">
            <label htmlFor="qTitle">Your question</label>
            <input
              id="qTitle"
              placeholder="e.g. Why do we use a for loop here?"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              minLength={5}
              maxLength={200}
              autoFocus
              required
            />
          </div>
          <div className="form-field">
            <label htmlFor="qBody">Details (optional)</label>
            <textarea id="qBody" rows={3} value={body} onChange={(e) => setBody(e.target.value)} maxLength={5000} />
          </div>
          <button type="submit" className="btn btn-primary" disabled={posting}>
            {posting && <span className="btn-spinner"></span>}
            Post question
          </button>
        </form>
      )}

      {loading && <SkeletonRows rows={3} />}

      {!loading && questions.length === 0 && (
        <div className="qa-empty">
          <MessageCircleQuestion size={28} />
          <p>
            {search
              ? "No questions match your search."
              : scope === "lecture"
                ? "No questions about this lecture yet. Be the first to ask!"
                : "No questions in this course yet."}
          </p>
        </div>
      )}

      {!loading && (
        <div className="question-list">
          {questions.map(function (question) {
            return (
              <button key={question.id} className="question-row" onClick={() => onOpenQuestion(question.id)}>
                <Avatar name={question.userName} imageUrl={question.userImageUrl} size={36} />
                <div className="question-row-text">
                  <strong>{question.title}</strong>
                  {question.body && <span className="question-row-body">{question.body}</span>}
                  <small>
                    {question.userName}
                    {scope === "course" && question.lectureTitle && (
                      <>
                        {" · "}
                        <PlayCircle size={12} /> {question.lectureTitle}
                      </>
                    )}
                    {" · "}
                    {timeAgo(question.createdAt)}
                  </small>
                </div>
                <div className="question-row-count">
                  {question.hasInstructorAnswer && <span className="badge badge-brand">Instructor answered</span>}
                  <small>
                    {question.answerCount} {question.answerCount === 1 ? "answer" : "answers"}
                  </small>
                </div>
              </button>
            );
          })}
        </div>
      )}
    </div>
  );
}

export default QuestionsPanel;
