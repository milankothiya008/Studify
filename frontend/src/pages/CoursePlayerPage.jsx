import { useEffect, useRef, useState } from "react";
import { Link, useParams } from "react-router-dom";
import api, { getErrorMessage } from "../api";
import ProgressBar from "../components/ProgressBar";
import ReviewForm from "../components/ReviewForm";
import Spinner from "../components/Spinner";
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

  const [player, setPlayer] = useState(null);
  const [currentLectureId, setCurrentLectureId] = useState(null);
  const [activeTab, setActiveTab] = useState("overview");
  const [error, setError] = useState("");

  // useRef keeps a value between renders without re-rendering the page.
  const lastSavedSecond = useRef(0);

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
  // (the tick of the lecture + the progress bar of the course).
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
      <div className="container page">
        <div className="empty-state">
          <h2>{error}</h2>
          <Link to={"/course/" + courseId} className="btn btn-primary">
            Go to the course page
          </Link>
        </div>
      </div>
    );
  }

  if (!player) {
    return <Spinner />;
  }

  const lectures = getAllLectures(player.sections);
  const currentIndex = lectures.findIndex((l) => l.id === currentLectureId);
  const currentLecture = currentIndex >= 0 ? lectures[currentIndex] : null;
  const previousLecture = currentIndex > 0 ? lectures[currentIndex - 1] : null;
  const nextLecture = currentIndex >= 0 && currentIndex < lectures.length - 1 ? lectures[currentIndex + 1] : null;

  function openLecture(lectureId) {
    setCurrentLectureId(lectureId);
    window.scrollTo(0, 0);
  }

  // ----- Video events -----

  function handleLoadedMetadata(event) {
    const video = event.target;
    // Resume from where the student stopped last time.
    const resumeAt = currentLecture.watchedSeconds;
    if (!currentLecture.isCompleted && resumeAt > 5 && resumeAt < video.duration - 5) {
      video.currentTime = resumeAt;
    }
    lastSavedSecond.current = Math.floor(video.currentTime);
  }

  function handleTimeUpdate(event) {
    const second = Math.floor(event.target.currentTime);
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
      {/* Top bar with the course progress */}
      <div className="player-topbar">
        <Link to={"/course/" + player.courseId} className="player-title">
          ‹ {player.title}
        </Link>
        {player.isEnrolled && (
          <div className="player-progress">
            <div className="player-progress-text">
              Your progress: {player.completedLectures} of {player.totalLectures} complete ({player.progressPercent}%)
            </div>
            <ProgressBar percent={player.progressPercent} />
          </div>
        )}
        {player.isCourseCompleted && (
          <Link to={"/certificate/" + player.courseId} className="btn btn-primary btn-small">
            🏆 Get certificate
          </Link>
        )}
      </div>

      {!player.isEnrolled && (
        <div className="preview-banner">Preview mode: you are not enrolled, so progress is not saved.</div>
      )}

      <div className="player-layout">
        <div className="player-main">
          <div className="video-box">
            {currentLecture && currentLecture.videoUrl && (
              <video
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
              <div className="no-video">This lecture has no video yet.</div>
            )}
            {!currentLecture && <div className="no-video">This course has no lectures yet.</div>}
          </div>

          <div className="player-nav">
            <button
              className="btn btn-outline"
              disabled={!previousLecture}
              onClick={() => openLecture(previousLecture.id)}
            >
              ‹ Previous
            </button>
            <span className="muted small">
              Lecture {currentIndex + 1} of {lectures.length}
            </span>
            <button className="btn btn-outline" disabled={!nextLecture} onClick={() => openLecture(nextLecture.id)}>
              Next ›
            </button>
          </div>

          {player.isCourseCompleted && (
            <div className="alert alert-success">
              🎉 Congratulations! You completed this course.{" "}
              <Link to={"/certificate/" + player.courseId}>View your certificate</Link>
            </div>
          )}

          <div className="tabs">
            <button className={activeTab === "overview" ? "tab active" : "tab"} onClick={() => setActiveTab("overview")}>
              Overview
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
                <p className="pre-line">{player.description}</p>
              </>
            )}
            {activeTab === "review" && <ReviewForm courseId={player.courseId} />}
          </div>
        </div>

        {/* Curriculum on the right */}
        <aside className="player-sidebar">
          <h3>Course content</h3>
          {player.sections.map(function (section, sectionIndex) {
            const doneInSection = section.lectures.filter((l) => l.isCompleted).length;
            return (
              <div key={section.id} className="player-section">
                <div className="player-section-title">
                  <strong>
                    Section {sectionIndex + 1}: {section.title}
                  </strong>
                  <small className="muted">
                    {doneInSection} / {section.lectures.length}
                  </small>
                </div>
                {section.lectures.map(function (lecture) {
                  const isCurrent = lecture.id === currentLectureId;
                  return (
                    <div
                      key={lecture.id}
                      className={isCurrent ? "player-lecture current" : "player-lecture"}
                      onClick={() => openLecture(lecture.id)}
                    >
                      <input
                        type="checkbox"
                        checked={lecture.isCompleted}
                        disabled={!player.isEnrolled}
                        title="Mark as complete"
                        onClick={(e) => e.stopPropagation()}
                        onChange={() => setCompleted(lecture.id, !lecture.isCompleted)}
                      />
                      <div>
                        <div>{lecture.title}</div>
                        <small className="muted">▶ {formatClock(lecture.durationSeconds)}</small>
                      </div>
                    </div>
                  );
                })}
              </div>
            );
          })}
        </aside>
      </div>
    </div>
  );
}

export default CoursePlayerPage;
