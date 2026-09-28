import { useEffect, useRef, useState } from "react";
import { Link, useParams, useSearchParams } from "react-router-dom";
import {
  ArrowLeft,
  Award,
  Check,
  ChevronDown,
  ChevronLeft,
  ChevronRight,
  ClipboardCheck,
  Eye,
  ListVideo,
  Lock,
  PlayCircle,
  VideoOff,
} from "lucide-react";
import api, { getErrorMessage } from "../api";
import ProgressRing from "../components/ProgressRing";
import ReviewForm from "../components/ReviewForm";
import Spinner from "../components/Spinner";
import EmptyState from "../components/EmptyState";
import QuestionsPanel from "../components/player/QuestionsPanel";
import NotesPanel from "../components/player/NotesPanel";
import QuizView from "../components/player/QuizView";
import { formatClock, getAllLectures } from "../utils";

// Save the video position every 10 seconds while it plays.
const SAVE_EVERY_SECONDS = 10;

// Chooses the lecture to open: the last one watched, or else the first one not completed.
function pickStartLecture(player) {
  const lectures = getAllLectures(player.sections);
  if (lectures.length === 0) {
    return null;
  }

  if (player.lastLectureId) {
    const last = lectures.find((l) => l.id === player.lastLectureId);
    if (last) {
      return last.id;
    }
  }

  const firstNotCompleted = lectures.find((l) => !l.isCompleted);
  if (firstNotCompleted) {
    return firstNotCompleted.id;
  }
  return lectures[0].id;
}

function CoursePlayerPage() {
  const { courseId } = useParams();
  const [searchParams] = useSearchParams();
  // A notification link like /learn/5?question=12 opens that question in the Q&A tab.
  const questionFromLink = searchParams.get("question");

  const [player, setPlayer] = useState(null);
  const [currentLectureId, setCurrentLectureId] = useState(null);
  const [currentQuizId, setCurrentQuizId] = useState(null); // a quiz is shown instead of a video
  const [activeTab, setActiveTab] = useState(questionFromLink ? "qa" : "overview");
  const [openQuestionId, setOpenQuestionId] = useState(questionFromLink ? Number(questionFromLink) : null);
  const [currentSecond, setCurrentSecond] = useState(0); // where the video is now (for notes)
  const [closedSections, setClosedSections] = useState({}); // sections collapsed in the sidebar
  const [error, setError] = useState("");

  // useRef keeps a value between renders without re-rendering the page.
  const lastSavedSecond = useRef(0);
  const videoRef = useRef(null);
  const jumpToSecond = useRef(null); // set by a note: start the next video at this second

  useEffect(
    function () {
      api
        .get("/learn/" + courseId)
        .then(function (response) {
          setPlayer(response.data);
          setCurrentLectureId(pickStartLecture(response.data));
        })
        .catch(function (err) {
          setError(getErrorMessage(err));
        });
    },
    [courseId]
  );

  // Copies the answer of the progress API into our page state
  // (the tick of the lecture + the progress of the course).
  function applyProgress(lectureId, data, watchedSeconds) {
    setPlayer(function (oldPlayer) {
      const newSections = oldPlayer.sections.map(function (section) {
        const newLectures = section.lectures.map(function (lecture) {
          if (lecture.id !== lectureId) {
            return lecture;
          }
          const changed = { ...lecture, isCompleted: data.isLectureCompleted };
          if (watchedSeconds !== null) {
            changed.watchedSeconds = watchedSeconds;
          }
          return changed;
        });
        return { ...section, lectures: newLectures };
      });

      return {
        ...oldPlayer,
        sections: newSections,
        completedLectures: data.completedLectures,
        progressPercent: data.progressPercent,
        isCourseCompleted: data.isCourseCompleted,
      };
    });
  }

  async function saveProgress(lectureId, seconds) {
    if (!player.isEnrolled) {
      return; // instructor preview: nothing is saved
    }
    try {
      const response = await api.post("/learn/lectures/" + lectureId + "/progress", { watchedSeconds: seconds });
      applyProgress(lectureId, response.data, seconds);
    } catch (err) {
      console.log("Could not save progress", err);
    }
  }

  async function setCompleted(lectureId, isCompleted) {
    if (!player.isEnrolled) {
      return;
    }
    try {
      const response = await api.post("/learn/lectures/" + lectureId + "/complete", { isCompleted: isCompleted });
      applyProgress(lectureId, response.data, null);
    } catch (err) {
      console.log("Could not save progress", err);
    }
  }

  if (error) {
    return (
      <div className="player-error">
        <EmptyState icon={Lock} title={error} text="Open the course page to enroll or renew your access.">
          <Link to={"/course/" + courseId} className="btn btn-primary">
            Go to the course page
          </Link>
          <Link to="/my-learning" className="btn btn-outline">
            My learning
          </Link>
        </EmptyState>
      </div>
    );
  }

  if (!player) {
    return <Spinner text="Loading course..." />;
  }

  const lectures = getAllLectures(player.sections);
  const currentIndex = lectures.findIndex((l) => l.id === currentLectureId);
  const currentLecture = currentIndex >= 0 ? lectures[currentIndex] : null;
  const previousLecture = currentIndex > 0 ? lectures[currentIndex - 1] : null;
  const nextLecture = currentIndex >= 0 && currentIndex < lectures.length - 1 ? lectures[currentIndex + 1] : null;

  function openLecture(lectureId) {
    // A jump time is only for a video; forget it if this lecture has none.
    const target = lectures.find((l) => l.id === lectureId);
    if (!target || !target.videoUrl) {
      jumpToSecond.current = null;
    }
    setCurrentQuizId(null);
    setCurrentLectureId(lectureId);
    setCurrentSecond(0);
    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  function openQuiz(quizId) {
    setCurrentQuizId(quizId);
    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  // A note was clicked: play that lecture from that second.
  function jumpTo(lectureId, seconds) {
    if (lectureId === currentLectureId && !currentQuizId && videoRef.current) {
      videoRef.current.currentTime = seconds;
      videoRef.current.play();
      window.scrollTo({ top: 0, behavior: "smooth" });
      return;
    }
    jumpToSecond.current = seconds;
    openLecture(lectureId);
  }

  // After a quiz: show the new best score in the sidebar.
  function applyQuizResult(quizId, result) {
    setPlayer(function (oldPlayer) {
      const newSections = oldPlayer.sections.map(function (section) {
        if (!section.quiz || section.quiz.id !== quizId) {
          return section;
        }
        const quiz = { ...section.quiz, bestScore: result.bestScore, passed: section.quiz.passed || result.passed };
        return { ...section, quiz: quiz };
      });
      return { ...oldPlayer, sections: newSections };
    });
  }

  function toggleSection(sectionId) {
    const copy = { ...closedSections };
    copy[sectionId] = !copy[sectionId];
    setClosedSections(copy);
  }

  // ----- Video events -----

  function handleLoadedMetadata(event) {
    const video = event.target;
    if (jumpToSecond.current !== null) {
      // Opened from a note: start at the note's second.
      video.currentTime = jumpToSecond.current;
      jumpToSecond.current = null;
    } else {
      // Resume from where the student stopped last time.
      const resumeAt = currentLecture.watchedSeconds;
      if (!currentLecture.isCompleted && resumeAt > 5 && resumeAt < video.duration - 5) {
        video.currentTime = resumeAt;
      }
    }
    lastSavedSecond.current = Math.floor(video.currentTime);
  }

  function handleTimeUpdate(event) {
    const second = Math.floor(event.target.currentTime);
    setCurrentSecond(second);
    if (Math.abs(second - lastSavedSecond.current) >= SAVE_EVERY_SECONDS) {
      lastSavedSecond.current = second;
      saveProgress(currentLecture.id, second);
    }
  }

  function handlePause(event) {
    const video = event.target;
    if (video.ended) {
      return; // handleEnded takes care of it
    }
    const second = Math.floor(video.currentTime);
    lastSavedSecond.current = second;
    saveProgress(currentLecture.id, second);
  }

  async function handleEnded() {
    await setCompleted(currentLecture.id, true);
    if (nextLecture) {
      openLecture(nextLecture.id); // go to the next lecture automatically
    }
  }

  return (
    <div className="player-page">
      {/* ---------- Top bar ---------- */}
      <div className="player-topbar">
        <Link to="/my-learning" className="icon-button icon-button-light" aria-label="Back to My learning">
          <ArrowLeft size={20} />
        </Link>
        <Link to={"/course/" + player.courseId} className="player-title">
          {player.title}
        </Link>

        {player.isEnrolled && (
          <div className="player-progress">
            <ProgressRing percent={player.progressPercent} size={42} />
            <div>
              <strong>Your progress</strong>
              <small>
                {player.completedLectures} of {player.totalLectures} lectures complete
              </small>
            </div>
          </div>
        )}

        {player.isCourseCompleted && (
          <Link to={"/certificate/" + player.courseId} className="btn btn-primary btn-small">
            <Award size={16} /> Certificate
          </Link>
        )}
      </div>

      {!player.isEnrolled && (
        <div className="preview-banner">
          <Eye size={16} /> Preview mode: you are not enrolled, so progress is not saved.
        </div>
      )}

      <div className="player-layout">
        <div className="player-main">
          {currentQuizId && (
            <div className="quiz-box">
              <QuizView quizId={currentQuizId} onFinished={(result) => applyQuizResult(currentQuizId, result)} />
            </div>
          )}

          <div className={currentQuizId ? "video-box hidden" : "video-box"}>
            {currentLecture && currentLecture.videoUrl && !currentQuizId && (
              <video
                ref={videoRef}
                key={currentLecture.id}
                src={currentLecture.videoUrl}
                controls
                autoPlay
                onLoadedMetadata={handleLoadedMetadata}
                onTimeUpdate={handleTimeUpdate}
                onPause={handlePause}
                onEnded={handleEnded}
              />
            )}
            {currentLecture && !currentLecture.videoUrl && (
              <div className="no-video">
                <VideoOff size={40} />
                <p>This lecture has no video yet.</p>
              </div>
            )}
            {!currentLecture && (
              <div className="no-video">
                <ListVideo size={40} />
                <p>This course has no lectures yet.</p>
              </div>
            )}
          </div>

          <div className={currentQuizId ? "player-nav hidden" : "player-nav"}>
            <button
              className="btn btn-outline btn-small"
              disabled={!previousLecture}
              onClick={() => openLecture(previousLecture.id)}
            >
              <ChevronLeft size={16} /> Previous
            </button>
            <span className="muted small">
              Lecture {currentIndex + 1} of {lectures.length}
            </span>
            <button className="btn btn-primary btn-small" disabled={!nextLecture} onClick={() => openLecture(nextLecture.id)}>
              Next <ChevronRight size={16} />
            </button>
          </div>

          <div className="player-content">
            {player.isCourseCompleted && (
              <div className="celebrate-banner">
                <Award size={28} />
                <div>
                  <strong>Congratulations! You completed this course.</strong>
                  <p>Your certificate is ready to view and print.</p>
                </div>
                <Link to={"/certificate/" + player.courseId} className="btn btn-white btn-small">
                  View certificate
                </Link>
              </div>
            )}

            <div className="tabs">
              <button className={activeTab === "overview" ? "tab active" : "tab"} onClick={() => setActiveTab("overview")}>
                Overview
              </button>
              <button className={activeTab === "qa" ? "tab active" : "tab"} onClick={() => setActiveTab("qa")}>
                Q&amp;A
              </button>
              <button className={activeTab === "notes" ? "tab active" : "tab"} onClick={() => setActiveTab("notes")}>
                Notes
              </button>
              {player.isEnrolled && (
                <button className={activeTab === "review" ? "tab active" : "tab"} onClick={() => setActiveTab("review")}>
                  Leave a rating
                </button>
              )}
            </div>

            <div className="tab-content">
              {activeTab === "overview" && (
                <>
                  {currentLecture && (
                    <>
                      <h2>{currentLecture.title}</h2>
                      {currentLecture.description && <p className="pre-line">{currentLecture.description}</p>}
                    </>
                  )}
                  <h3>About this course</h3>
                  <p className="muted">By {player.instructorName}</p>
                  {player.description && <p className="pre-line">{player.description}</p>}
                </>
              )}
              {activeTab === "qa" && (
                <QuestionsPanel
                  courseId={player.courseId}
                  lecture={currentLecture}
                  openQuestionId={openQuestionId}
                  onOpenQuestion={setOpenQuestionId}
                />
              )}
              {activeTab === "notes" && (
                <NotesPanel
                  courseId={player.courseId}
                  lecture={currentLecture}
                  currentSecond={currentSecond}
                  canSave={player.isEnrolled}
                  onJump={jumpTo}
                />
              )}
              {activeTab === "review" && <ReviewForm courseId={player.courseId} />}
            </div>
          </div>
        </div>

        {/* ---------- Curriculum on the right ---------- */}
        <aside className="player-sidebar">
          <h3>Course content</h3>
          {player.sections.map(function (section, sectionIndex) {
            const doneInSection = section.lectures.filter((l) => l.isCompleted).length;
            const isClosed = Boolean(closedSections[section.id]);
            return (
              <div key={section.id} className={isClosed ? "player-section" : "player-section open"}>
                <button className="player-section-title" onClick={() => toggleSection(section.id)}>
                  <span>
                    <strong>
                      Section {sectionIndex + 1}: {section.title}
                    </strong>
                    <small>
                      {doneInSection} / {section.lectures.length} complete
                    </small>
                  </span>
                  <ChevronDown size={18} className="accordion-chevron" />
                </button>

                <div className="accordion-body">
                  <div>
                    {section.lectures.map(function (lecture) {
                      const isCurrent = lecture.id === currentLectureId && !currentQuizId;
                      let className = "player-lecture";
                      if (isCurrent) className = className + " current";
                      if (lecture.isCompleted) className = className + " done";

                      return (
                        <div key={lecture.id} className={className} onClick={() => openLecture(lecture.id)}>
                          <button
                            className="check-circle"
                            title={lecture.isCompleted ? "Mark as not complete" : "Mark as complete"}
                            disabled={!player.isEnrolled}
                            onClick={(e) => {
                              e.stopPropagation();
                              setCompleted(lecture.id, !lecture.isCompleted);
                            }}
                          >
                            {lecture.isCompleted && <Check size={14} />}
                          </button>
                          <div className="player-lecture-text">
                            <span>{lecture.title}</span>
                            <small>
                              <PlayCircle size={13} /> {formatClock(lecture.durationSeconds)}
                            </small>
                          </div>
                        </div>
                      );
                    })}

                    {section.quiz && (
                      <div
                        className={currentQuizId === section.quiz.id ? "player-lecture player-quiz current" : "player-lecture player-quiz"}
                        onClick={() => openQuiz(section.quiz.id)}
                      >
                        <span className={section.quiz.passed ? "quiz-badge passed" : "quiz-badge"}>
                          <ClipboardCheck size={14} />
                        </span>
                        <div className="player-lecture-text">
                          <span>Quiz: {section.quiz.title}</span>
                          <small>
                            {section.quiz.questionCount} questions
                            {section.quiz.bestScore !== null && section.quiz.bestScore !== undefined &&
                              " · best " + section.quiz.bestScore + "%"}
                          </small>
                        </div>
                      </div>
                    )}
                  </div>
                </div>
              </div>
            );
          })}
        </aside>
      </div>
    </div>
  );
}

export default CoursePlayerPage;
