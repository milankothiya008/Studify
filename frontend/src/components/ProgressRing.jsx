// A round progress indicator with the percentage in the middle.
function ProgressRing({ percent, size }) {
  const ringSize = size || 44;
  const stroke = 4;
  const radius = (ringSize - stroke) / 2;
  const circumference = 2 * Math.PI * radius;
  const safePercent = Math.max(0, Math.min(100, Number(percent) || 0));
  const offset = circumference - (safePercent / 100) * circumference;

  return (
    <div className="progress-ring" style={{ width: ringSize, height: ringSize }}>
      <svg width={ringSize} height={ringSize}>
        <circle className="progress-ring-track" cx={ringSize / 2} cy={ringSize / 2} r={radius} strokeWidth={stroke} />
        <circle
          className="progress-ring-fill"
          cx={ringSize / 2}
          cy={ringSize / 2}
          r={radius}
          strokeWidth={stroke}
          strokeDasharray={circumference}
          strokeDashoffset={offset}
        />
      </svg>
      <span>{safePercent}%</span>
    </div>
  );
}

export default ProgressRing;
