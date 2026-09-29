// Small helper functions used by many pages.

// Currency used for all prices. Change these two lines to use another currency.
const CURRENCY_SYMBOL = "₹";
const NUMBER_LOCALE = "en-IN";

// 1999 -> "₹1,999"   0 -> "Free"
export function formatPrice(price) {
  if (Number(price) === 0) {
    return "Free";
  }
  return formatMoney(price);
}

// Same as formatPrice, but 0 -> "₹0" (used on receipts)
export function formatMoney(amount) {
  const number = Number(amount);
  // Whole rupees without decimals (₹1,999), otherwise always 2 decimals (₹1,599.20).
  const decimals = number % 1 === 0 ? 0 : 2;
  return CURRENCY_SYMBOL + number.toLocaleString(NUMBER_LOCALE, {
    minimumFractionDigits: decimals,
    maximumFractionDigits: 2,
  });
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

// "2 minutes ago", "3 hours ago", "5 days ago", or the date for older things.
export function timeAgo(dateText) {
  const seconds = Math.floor((Date.now() - new Date(dateText).getTime()) / 1000);
  if (seconds < 60) {
    return "just now";
  }
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) {
    return minutes + (minutes === 1 ? " minute ago" : " minutes ago");
  }
  const hours = Math.floor(minutes / 60);
  if (hours < 24) {
    return hours + (hours === 1 ? " hour ago" : " hours ago");
  }
  const days = Math.floor(hours / 24);
  if (days < 30) {
    return days + (days === 1 ? " day ago" : " days ago");
  }
  return formatDate(dateText);
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

// Upload progress from axios' onUploadProgress, as a whole number from 0 to 100.
// Some browsers don't send the total size; then we show 0 instead of "NaN%".
export function getUploadPercent(progressEvent) {
  if (!progressEvent.total) {
    return 0;
  }
  return Math.round((progressEvent.loaded * 100) / progressEvent.total);
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

// Short numbers for counters: 950 -> "950", 1250 -> "1.3K", 2400000 -> "2.4M"
export function formatCount(number) {
  if (number >= 1000000) {
    return (number / 1000000).toFixed(1).replace(".0", "") + "M";
  }
  if (number >= 1000) {
    return (number / 1000).toFixed(1).replace(".0", "") + "K";
  }
  return String(number);
}
