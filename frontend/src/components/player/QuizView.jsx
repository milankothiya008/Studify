import { useEffect, useState } from "react";
import { Award, CheckCircle2, ClipboardCheck, RotateCcw, XCircle } from "lucide-react";
import api, { getErrorMessage } from "../../api";
import { useToast } from "../../ToastContext";
import Spinner from "../Spinner";

const LETTERS = ["A", "B", "C", "D"];

// Takes a quiz: questions -> submit -> score with the right answers and explanations.
// onFinished(result) tells the player page the new best score.
function QuizView({ quizId, onFinished }) {
  const showToast = useToast();
  const [quiz, setQuiz] = useState(null);
  const [answers, setAnswers] = useState({}); // questionId -> chosen option (1-4)
  const [result, setResult] = useState(null);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");

  useEffect(
    function () {
      setQuiz(null);
      setResult(null);
      setAnswers({});
      api
        .get("/learn/quizzes/" + quizId)
        .then(function (response) {
          setQuiz(response.data);
        })
        .catch(function (err) {
          setError(getErrorMessage(err));
        });
    },
    [quizId]
  );

  function choose(questionId, option) {
    if (result) {
      return; // answers are locked after submitting
    }
    const copy = { ...answers };
    copy[questionId] = option;
    setAnswers(copy);
  }

  async function handleSubmit() {
    setSubmitting(true);
    try {
      const body = {
        answers: quiz.questions.map(function (question) {
          return { questionId: question.id, selectedOption: answers[question.id] || 0 };
        }),
      };
      const response = await api.post("/learn/quizzes/" + quizId + "/attempts", body);
      setResult(response.data);
      onFinished(response.data);
      window.scrollTo({ top: 0, behavior: "smooth" });
    } catch (err) {
      showToast(getErrorMessage(err), "error");
    }
    setSubmitting(false);
  }

  function tryAgain() {
    setResult(null);
    setAnswers({});
    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  if (error) {
    return <div className="alert alert-error">{error}</div>;
  }

  if (!quiz) {
    return <Spinner text="Loading quiz..." />;
  }

  if (quiz.questions.length === 0) {
    return <p className="muted">This quiz has no questions yet.</p>;
  }

  const answeredCount = Object.keys(answers).length;

  // The result of each question, once submitted.
  function resultFor(questionId) {
    if (!result) {
      return null;
    }
    return result.results.find((r) => r.questionId === questionId);
  }

  return (
    <div className="quiz">
      <div className="quiz-header">
        <span className="quiz-icon">
          <ClipboardCheck size={26} />
        </span>
        <div>
          <small>{quiz.sectionTitle}</small>
          <h2>{quiz.title}</h2>
          <p className="muted small">
            {quiz.questions.length} questions · pass with {quiz.passPercent}%
            {quiz.bestScore !== null && quiz.bestScore !== undefined && " · your best: " + quiz.bestScore + "%"}
          </p>
        </div>
      </div>

      {result && (
        <div className={result.passed ? "quiz-result passed" : "quiz-result failed"}>
          {result.passed ? <Award size={34} /> : <RotateCcw size={34} />}
          <div>
            <strong>
              {result.passed ? "You passed!" : "Not quite there yet"} · {result.scorePercent}%
            </strong>
            <p>
              {result.correctCount} of {result.totalQuestions} correct.{" "}
              {result.passed ? "Great work." : "You need " + result.passPercent + "% to pass. Review the answers below."}
            </p>
          </div>
          <button className="btn btn-white btn-small" onClick={tryAgain}>
            <RotateCcw size={16} /> Try again
          </button>
        </div>
      )}

      {quiz.questions.map(function (question, index) {
        const questionResult = resultFor(question.id);
        return (
          <div key={question.id} className="quiz-question card">
            <p className="quiz-question-text">
              <span className="quiz-number">{index + 1}</span> {question.text}
            </p>

            <div className="quiz-options">
              {question.options.map(function (option, optionIndex) {
                const optionNumber = optionIndex + 1;
                let className = "quiz-option";
                if (answers[question.id] === optionNumber) className = className + " chosen";
                if (questionResult && questionResult.correctOption === optionNumber) className = className + " correct";
                if (questionResult && questionResult.selectedOption === optionNumber && !questionResult.isCorrect) {
                  className = className + " wrong";
                }

                return (
                  <button
                    key={optionIndex}
                    type="button"
                    className={className}
                    onClick={() => choose(question.id, optionNumber)}
                    disabled={Boolean(result)}
                  >
                    <span className="quiz-letter">{LETTERS[optionIndex]}</span>
                    <span>{option}</span>
                    {questionResult && questionResult.correctOption === optionNumber && <CheckCircle2 size={18} />}
                    {questionResult && questionResult.selectedOption === optionNumber && !questionResult.isCorrect && (
                      <XCircle size={18} />
                    )}
                  </button>
                );
              })}
            </div>

            {questionResult && questionResult.explanation && (
              <p className="quiz-explanation">
                <strong>{questionResult.isCorrect ? "Correct. " : "Explanation: "}</strong>
                {questionResult.explanation}
              </p>
            )}
          </div>
        );
      })}

      {!result && (
        <div className="quiz-submit">
          <span className="muted small">
            {answeredCount} of {quiz.questions.length} answered
          </span>
          <button className="btn btn-primary btn-large" onClick={handleSubmit} disabled={submitting || answeredCount === 0}>
            {submitting && <span className="btn-spinner"></span>}
            Submit answers
          </button>
        </div>
      )}
    </div>
  );
}

export default QuizView;
