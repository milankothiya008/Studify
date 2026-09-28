import { useEffect, useRef, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { Bell, CheckCheck } from "lucide-react";
import api from "../api";
import { timeAgo } from "../utils";

// Checks for new notifications every minute (and on every page change).
const REFRESH_EVERY_MS = 60000;

// The bell icon in the navbar, with the number of unread notifications.
function NotificationBell() {
  const navigate = useNavigate();
  const location = useLocation();
  const [data, setData] = useState({ unreadCount: 0, items: [] });
  const [open, setOpen] = useState(false);
  const boxRef = useRef(null);

  function load() {
    api
      .get("/notifications")
      .then(function (response) {
        setData(response.data);
      })
      .catch(function () {
        // not important: try again next time
      });
  }

  // Load now, on every page change, and every minute.
  useEffect(
    function () {
      load();
      const timer = setInterval(load, REFRESH_EVERY_MS);
      return function () {
        clearInterval(timer);
      };
    },
    [location.pathname]
  );

  // Close when clicking or tapping outside.
  useEffect(function () {
    function handlePointerDown(event) {
      if (boxRef.current && !boxRef.current.contains(event.target)) {
        setOpen(false);
      }
    }
    document.addEventListener("pointerdown", handlePointerDown);
    return function () {
      document.removeEventListener("pointerdown", handlePointerDown);
    };
  }, []);

  async function openNotification(notification) {
    setOpen(false);
    if (!notification.isRead) {
      await api.post("/notifications/" + notification.id + "/read");
      load();
    }
    if (notification.link) {
      navigate(notification.link);
    }
  }

  async function markAllRead() {
    await api.post("/notifications/read-all");
    load();
  }

  return (
    <div className="bell" ref={boxRef}>
      <button
        className="icon-button bell-button"
        onClick={() => setOpen(!open)}
        aria-label={"Notifications, " + data.unreadCount + " unread"}
      >
        <Bell size={21} />
        {data.unreadCount > 0 && (
          <span className="bell-count">{data.unreadCount > 9 ? "9+" : data.unreadCount}</span>
        )}
      </button>

      {open && (
        <div className="dropdown bell-dropdown">
          <div className="bell-header">
            <strong>Notifications</strong>
            {data.unreadCount > 0 && (
              <button className="link-button small" onClick={markAllRead}>
                <CheckCheck size={15} /> Mark all as read
              </button>
            )}
          </div>

          {data.items.length === 0 && <p className="bell-empty">You're all caught up.</p>}

          <div className="bell-list">
            {data.items.map(function (notification) {
              return (
                <button
                  key={notification.id}
                  className={notification.isRead ? "bell-item" : "bell-item unread"}
                  onClick={() => openNotification(notification)}
                >
                  <span className="bell-dot"></span>
                  <span className="bell-text">
                    <strong>{notification.title}</strong>
                    <span>{notification.message}</span>
                    <small>{timeAgo(notification.createdAt)}</small>
                  </span>
                </button>
              );
            })}
          </div>
        </div>
      )}
    </div>
  );
}

export default NotificationBell;
