import { useState } from "react";

// A simple column chart (one bar per day) drawn with SVG, no chart library.
//   data        = [{ label: "2026-09-28", value: 3 }, ...]
//   formatValue = function that turns a number into text, e.g. 1999 -> "₹1,999"
//   title       = what the chart shows (there is only one series, so no legend is needed)
//   wholeNumbers = true for counts (students), so the axis never shows halves
const WIDTH = 720;
const HEIGHT = 220;
const PAD_LEFT = 52;
const PAD_RIGHT = 12;
const PAD_TOP = 14;
const PAD_BOTTOM = 30;
const MAX_BAR = 24; // bars never get thicker than 24px

// A round number just above the maximum, so the y axis shows clean values (0, 5, 10 ...).
function niceMax(value) {
  if (value <= 0) {
    return 4;
  }
  const power = Math.pow(10, Math.floor(Math.log10(value)));
  const steps = [1, 2, 2.5, 5, 10];
  for (let i = 0; i < steps.length; i++) {
    if (steps[i] * power >= value) {
      return steps[i] * power;
    }
  }
  return 10 * power;
}

// "2026-09-28" -> "28 Sep"
function shortDate(label) {
  return new Date(label + "T00:00:00").toLocaleDateString("en-GB", { day: "numeric", month: "short" });
}

// A bar with 4px rounded corners at the top and a square bottom on the baseline.
function barPath(x, y, width, height) {
  const r = Math.min(4, width / 2, height);
  const bottom = y + height;
  return (
    "M" + x + "," + bottom +
    " L" + x + "," + (y + r) +
    " Q" + x + "," + y + " " + (x + r) + "," + y +
    " L" + (x + width - r) + "," + y +
    " Q" + (x + width) + "," + y + " " + (x + width) + "," + (y + r) +
    " L" + (x + width) + "," + bottom + " Z"
  );
}

function ColumnChart({ data, formatValue, title, wholeNumbers }) {
  const [hovered, setHovered] = useState(null); // index of the bar under the mouse

  const plotWidth = WIDTH - PAD_LEFT - PAD_RIGHT;
  const plotHeight = HEIGHT - PAD_TOP - PAD_BOTTOM;
  const highest = Math.max(...data.map((d) => d.value), 0);
  let maxValue = niceMax(highest);
  if (wholeNumbers) {
    // A multiple of 4, so the 4 grid steps are whole numbers too.
    maxValue = Math.max(4, Math.ceil(highest / 4) * 4);
  }
  const slot = plotWidth / data.length;
  const barWidth = Math.max(2, Math.min(MAX_BAR, slot - 2)); // at least a 2px gap between bars

  // Four horizontal grid lines with their values.
  const ticks = [0, 0.25, 0.5, 0.75, 1].map((part) => part * maxValue);

  // Only a few date labels, so they never overlap.
  const labelEvery = Math.ceil(data.length / 6);

  const total = data.reduce((sum, d) => sum + d.value, 0);
  const hoveredItem = hovered !== null ? data[hovered] : null;

  return (
    <figure className="chart">
      <figcaption className="chart-title">
        <span>{title}</span>
        <strong>{formatValue(total)}</strong>
      </figcaption>

      <div className="chart-area">
        <svg viewBox={"0 0 " + WIDTH + " " + HEIGHT} role="img" aria-label={title + ", total " + formatValue(total)}>
          {/* grid lines and y axis values */}
          {ticks.map(function (tick) {
            const y = PAD_TOP + plotHeight - (tick / maxValue) * plotHeight;
            return (
              <g key={tick}>
                <line x1={PAD_LEFT} x2={WIDTH - PAD_RIGHT} y1={y} y2={y} className="chart-gridline" />
                <text x={PAD_LEFT - 8} y={y + 4} className="chart-axis" textAnchor="end">
                  {formatValue(tick)}
                </text>
              </g>
            );
          })}

          {/* bars */}
          {data.map(function (item, index) {
            const height = (item.value / maxValue) * plotHeight;
            const x = PAD_LEFT + index * slot + (slot - barWidth) / 2;
            const y = PAD_TOP + plotHeight - height;
            return (
              <g key={item.label}>
                {item.value > 0 && (
                  <path d={barPath(x, y, barWidth, height)} className={hovered === index ? "chart-bar active" : "chart-bar"} />
                )}
                {/* invisible full-height area: easier to hover than a thin bar */}
                <rect
                  x={PAD_LEFT + index * slot}
                  y={PAD_TOP}
                  width={slot}
                  height={plotHeight}
                  className="chart-hit"
                  tabIndex={0}
                  aria-label={shortDate(item.label) + ": " + formatValue(item.value)}
                  onPointerEnter={() => setHovered(index)}
                  onPointerLeave={() => setHovered(null)}
                  onFocus={() => setHovered(index)}
                  onBlur={() => setHovered(null)}
                />
                {index % labelEvery === 0 && (
                  <text x={PAD_LEFT + index * slot + slot / 2} y={HEIGHT - 8} className="chart-axis" textAnchor="middle">
                    {shortDate(item.label)}
                  </text>
                )}
              </g>
            );
          })}

          {/* baseline */}
          <line x1={PAD_LEFT} x2={WIDTH - PAD_RIGHT} y1={PAD_TOP + plotHeight} y2={PAD_TOP + plotHeight} className="chart-baseline" />
        </svg>

        {hoveredItem && (
          <div
            className="chart-tooltip"
            style={{
              // Centred above the hovered bar, but never outside the chart.
              left: Math.min(88, Math.max(12, ((PAD_LEFT + hovered * slot + slot / 2) / WIDTH) * 100)) + "%",
              top: ((PAD_TOP + plotHeight - (hoveredItem.value / maxValue) * plotHeight) / HEIGHT) * 100 + "%",
            }}
          >
            <strong>{formatValue(hoveredItem.value)}</strong>
            <span>{shortDate(hoveredItem.label)}</span>
          </div>
        )}
      </div>
    </figure>
  );
}

export default ColumnChart;
