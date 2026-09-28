import { useEffect, useState } from "react";
import { CheckCircle2, MessageCircleQuestion, MessagesSquare, PlayCircle } from "lucide-react";
import api from "../../api";
import PageHeader from "../../components/PageHeader";
import EmptyState from "../../components/EmptyState";
import Avatar from "../../components/Avatar";
import QuestionThread from "../../components/QuestionThread";
import { SkeletonRows } from "../../components/Skeleton";
import { timeAgo } from "../../utils";

// The instructor's inbox: questions students asked in all their courses.
function InstructorQuestionsPage() {
  const [filter, setFilter] = useState("unanswered");
  const [questions, setQuestions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [openQuestionId, setOpenQuestionId] = useState(null);

  function load() {
    setLoading(true);
    api
      .get("/instructor/questions", { params: { unanswered: filter === "unanswered" } })
      .then(function (response) {
        setQuestions(response.data);
      })
      .finally(function () {
        setLoading(false);
      });
  }

  useEffect(
    function () {
      load();
    },
    [filter]
  );

  return (
    <div>
      <PageHeader title="Student questions" subtitle="Answer questions from students in all your courses." />

      <div className="container page">
        {openQuestionId ? (
          <div className="card card-padded">
            <QuestionThread
              questionId={openQuestionId}
              onBack={() => {
                setOpenQuestionId(null);
                load();
              }}
              onChanged={load}
            />
          </div>
        ) : (
          <>
            <div className="tabs">
              <button
                className={filter === "unanswered" ? "tab active" : "tab"}
                onClick={() => setFilter("unanswered")}
              >
                <MessageCircleQuestion size={16} /> Waiting for your answer
              </button>
              <button className={filter === "all" ? "tab active" : "tab"} onClick={() => setFilter("all")}>
                <MessagesSquare size={16} /> All questions
              </button>
            </div>

            <div className="tab-content">
              {loading && <SkeletonRows rows={4} />}

              {!loading && questions.length === 0 && (
                <EmptyState
                  icon={CheckCircle2}
                  title={filter === "unanswered" ? "You're all caught up!" : "No questions yet"}
                  text={
                    filter === "unanswered"
                      ? "Every question has an answer from you."
                      : "When students ask questions in your courses, they appear here."
                  }
                />
              )}

              {!loading && (
                <div className="question-list">
                  {questions.map(function (question, index) {
                    return (
                      <button
                        key={question.id}
                        className="question-row card stagger-item"
                        style={{ "--i": index }}
                        onClick={() => setOpenQuestionId(question.id)}
                      >
                        <Avatar name={question.userName} imageUrl={question.userImageUrl} size={40} />
                        <div className="question-row-text">
                          <strong>{question.title}</strong>
                          {question.body && <span className="question-row-body">{question.body}</span>}
                          <small>
                            {question.userName} · {question.courseTitle}
                            {question.lectureTitle && (
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
                          {question.hasInstructorAnswer ? (
                            <span className="badge badge-success">Answered</span>
                          ) : (
                            <span className="badge badge-warning">Needs answer</span>
                          )}
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
          </>
        )}
      </div>
    </div>
  );
}

export default InstructorQuestionsPage;
