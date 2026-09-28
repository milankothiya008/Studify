import { useEffect, useState } from "react";
import { ArrowLeft, BadgeCheck, PlayCircle, Trash2 } from "lucide-react";
import api, { getErrorMessage } from "../api";
import { useToast } from "../ToastContext";
import Avatar from "./Avatar";
import { SkeletonRows } from "./Skeleton";
import { timeAgo } from "../utils";

// One question with all its answers and a reply box.
// Used in the course player (Q&A tab) and in the instructor's questions inbox.
function QuestionThread({ questionId, onBack, onChanged }) {
  const showToast = useToast();
  const [thread, setThread] = useState(null);
  const [reply, setReply] = useState("");
  const [sending, setSending] = useState(false);
  const [error, setError] = useState("");

  function load() {
    api
      .get("/questions/" + questionId)
      .then(function (response) {
        setThread(response.data);
      })
      .catch(function (err) {
        setError(getErrorMessage(err));
      });
  }

  useEffect(
    function () {
      load();
    },
    [questionId]
  );

  async function handleReply(event) {
    event.preventDefault();
    setSending(true);
    try {
      await api.post("/questions/" + questionId + "/answers", { body: reply });
      setReply("");
      showToast("Your answer was posted.");
      load();
      if (onChanged) onChanged();
    } catch (err) {
      showToast(getErrorMessage(err), "error");
    }
    setSending(false);
  }

  async function deleteQuestion() {
    if (!window.confirm("Delete this question and all its answers?")) {
      return;
    }
    try {
      await api.delete("/questions/" + questionId);
      showToast("Question deleted.");
      if (onChanged) onChanged();
      onBack();
    } catch (err) {
      showToast(getErrorMessage(err), "error");
    }
  }

  async function deleteAnswer(answerId) {
    if (!window.confirm("Delete this answer?")) {
      return;
    }
    try {
      await api.delete("/answers/" + answerId);
      load();
      if (onChanged) onChanged();
    } catch (err) {
      showToast(getErrorMessage(err), "error");
    }
  }

  if (error) {
    return (
      <div>
        <button className="link-button" onClick={onBack}>
          <ArrowLeft size={16} /> Back
        </button>
        <div className="alert alert-error">{error}</div>
      </div>
    );
  }

  if (!thread) {
    return <SkeletonRows rows={3} />;
  }

  const question = thread.question;

  return (
    <div className="thread">
      <button className="link-button" onClick={onBack}>
        <ArrowLeft size={16} /> All questions
      </button>

      <div className="thread-question">
        <Avatar name={question.userName} imageUrl={question.userImageUrl} size={44} />
        <div className="thread-body">
          <h3>{question.title}</h3>
          <p className="thread-meta">
            {question.userName} · {timeAgo(question.createdAt)}
            {question.lectureTitle && (
              <span className="thread-lecture">
                <PlayCircle size={13} /> {question.lectureTitle}
              </span>
            )}
          </p>
          {question.body && <p className="pre-line">{question.body}</p>}
          {thread.canDelete && (
            <button className="link-button danger small" onClick={deleteQuestion}>
              <Trash2 size={14} /> Delete question
            </button>
          )}
        </div>
      </div>

      <h4 className="thread-count">
        {thread.answers.length} {thread.answers.length === 1 ? "answer" : "answers"}
      </h4>

      <div className="answer-list">
        {thread.answers.map(function (answer) {
          return (
            <div key={answer.id} className={answer.isInstructor ? "answer instructor-answer" : "answer"}>
              <Avatar name={answer.userName} imageUrl={answer.userImageUrl} size={36} />
              <div className="thread-body">
                <p className="thread-meta">
                  <strong>{answer.userName}</strong>
                  {answer.isInstructor && (
                    <span className="badge badge-brand">
                      <BadgeCheck size={13} /> Instructor
                    </span>
                  )}
                  <span>{timeAgo(answer.createdAt)}</span>
                </p>
                <p className="pre-line">{answer.body}</p>
                {answer.canDelete && (
                  <button className="link-button danger small" onClick={() => deleteAnswer(answer.id)}>
                    <Trash2 size={14} /> Delete
                  </button>
                )}
              </div>
            </div>
          );
        })}
      </div>

      <form className="reply-form" onSubmit={handleReply}>
        <textarea
          rows={3}
          placeholder="Write your answer..."
          value={reply}
          onChange={(e) => setReply(e.target.value)}
          maxLength={5000}
          required
        />
        <button type="submit" className="btn btn-primary" disabled={sending || reply.trim().length < 2}>
          {sending && <span className="btn-spinner"></span>}
          Post answer
        </button>
      </form>
    </div>
  );
}

export default QuestionThread;
