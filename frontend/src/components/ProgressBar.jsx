// A thin bar that shows a percentage (0 - 100).
function ProgressBar({ percent }) {
  return (
    <div className="progress">
      <div className="progress-fill" style={{ width: percent + "%" }}></div>
    </div>
  );
}

export default ProgressBar;
