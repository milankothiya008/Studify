// A thin bar that shows a percentage (0 - 100).
function ProgressBar({ percent }) {
  const safePercent = Math.max(0, Math.min(100, Number(percent) || 0));
  return (
    <div className="progress" role="progressbar" aria-valuenow={safePercent} aria-valuemin={0} aria-valuemax={100}>
      <div className="progress-fill" style={{ width: safePercent + "%" }}></div>
    </div>
  );
}

export default ProgressBar;
