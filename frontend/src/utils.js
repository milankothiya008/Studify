// Small helper functions used by many pages.

// Currency used for all prices. Change these two lines to use another currency.
const CURRENCY_SYMBOL = "₹";
const NUMBER_LOCALE = "en-IN";

// 1999 -> "₹1,999"   0 -> "Free"
export function formatPrice(price) {
  if (Number(price) === 0) {
    return "Free";
  }
  return CURRENCY_SYMBOL + Number(price).toLocaleString(NUMBER_LOCALE);
}

// Same as formatPrice, but 0 -> "₹0" (used on receipts)
export function formatMoney(amount) {
  return CURRENCY_SYMBOL + Number(amount).toLocaleString(NUMBER_LOCALE);
}

// Length of one lecture: 75 -> "1:15"
export function formatClock(totalSeconds) {
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return minutes + ":" + String(seconds).padStart(2, "0");
}

// Length of a whole course: 3900 -> "1h 5m", 125 -> "2m", 40 -> "40s"
export function formatDuration(totalSeconds) {
  const hours = Math.floor(totalSeconds / 3600);
  const minutes = Math.floor((totalSeconds % 3600) / 60);

  if (hours > 0) {
    return hours + "h " + minutes + "m";
  }
  if (minutes > 0) {
    return minutes + "m";
  }
  return totalSeconds + "s";
}

// "2026-09-27T14:24:55Z" -> "27 Sept 2026"
export function formatDate(dateText) {
  if (!dateText) {
    return "";
  }
  return new Date(dateText).toLocaleDateString("en-GB", {
    day: "numeric",
    month: "short",
    year: "numeric",
  });
}

// Text with one item per line -> array of non-empty lines
export function splitLines(text) {
  if (!text) {
    return [];
  }
  return text
    .split("\n")
    .map(function (line) {
      return line.trim();
    })
    .filter(function (line) {
      return line !== "";
    });
}

// "Grace Hopper" -> "GH" (used when a user has no profile photo)
export function getInitials(name) {
  if (!name) {
    return "?";
  }
  const parts = name.trim().split(" ");
  let initials = parts[0].charAt(0);
  if (parts.length > 1) {
    initials = initials + parts[parts.length - 1].charAt(0);
  }
  return initials.toUpperCase();
}

// Reads the length of a video file in the browser, before uploading it.
// Returns a Promise with the number of seconds (0 if it cannot be read).
export function getVideoDuration(file) {
  return new Promise(function (resolve) {
    const video = document.createElement("video");
    video.preload = "metadata";
    video.onloadedmetadata = function () {
      URL.revokeObjectURL(video.src);
      resolve(Math.round(video.duration) || 0);
    };
    video.onerror = function () {
      resolve(0);
    };
    video.src = URL.createObjectURL(file);
  });
}

// Every lecture of a course in one flat list (sections one after another).
export function getAllLectures(sections) {
  const lectures = [];
  sections.forEach(function (section) {
    section.lectures.forEach(function (lecture) {
      lectures.push(lecture);
    });
  });
  return lectures;
}
