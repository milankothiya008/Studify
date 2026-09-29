import { useEffect, useState } from "react";

// A number that counts up from 0 when it appears (e.g. 0 -> 1,250).
// decimals = how many digits after the dot (1 for a rating like 4.6).
function CountUp({ value, decimals }) {
  const [shown, setShown] = useState(0);
  const places = decimals || 0;

  useEffect(
    function () {
      // People who turned off animations in their system settings see the number at once.
      const reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
      if (reduceMotion || value === 0) {
        setShown(value);
        return;
      }

      const duration = 1100; // milliseconds
      const start = performance.now();
      let frame;

      function step(now) {
        const progress = Math.min((now - start) / duration, 1);
        const eased = 1 - Math.pow(1 - progress, 3); // starts fast, ends slowly
        setShown(value * eased);
        if (progress < 1) {
          frame = requestAnimationFrame(step);
        }
      }
      frame = requestAnimationFrame(step);

      return function () {
        cancelAnimationFrame(frame);
      };
    },
    [value]
  );

  const rounded = places > 0 ? shown.toFixed(places) : Math.round(shown).toLocaleString("en-IN");
  return <span>{rounded}</span>;
}

export default CountUp;
