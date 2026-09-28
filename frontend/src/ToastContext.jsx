import { createContext, useContext, useState } from "react";
import { CheckCircle2, Info, X, XCircle } from "lucide-react";

// Small pop-up messages in the corner ("Saved!", "Payment successful" ...).
// Any page can show one:
//   const showToast = useToast();
//   showToast("Saved!");              -> green
//   showToast("Something failed", "error");  -> red
const ToastContext = createContext(null);

let nextToastId = 1;

export function ToastProvider({ children }) {
  const [toasts, setToasts] = useState([]);

  function removeToast(id) {
    setToasts(function (oldToasts) {
      return oldToasts.filter((toast) => toast.id !== id);
    });
  }

  function showToast(message, type) {
    const id = nextToastId;
    nextToastId++;

    setToasts(function (oldToasts) {
      return [...oldToasts, { id: id, message: message, type: type || "success" }];
    });

    // Hide it automatically after 4 seconds.
    setTimeout(function () {
      removeToast(id);
    }, 4000);
  }

  return (
    <ToastContext.Provider value={showToast}>
      {children}

      <div className="toast-container">
        {toasts.map(function (toast) {
          let icon = <CheckCircle2 size={20} />;
          if (toast.type === "error") {
            icon = <XCircle size={20} />;
          } else if (toast.type === "info") {
            icon = <Info size={20} />;
          }

          return (
            <div key={toast.id} className={"toast toast-" + toast.type} role="status">
              {icon}
              <span>{toast.message}</span>
              <button className="toast-close" onClick={() => removeToast(toast.id)} aria-label="Close">
                <X size={16} />
              </button>
            </div>
          );
        })}
      </div>
    </ToastContext.Provider>
  );
}

export function useToast() {
  return useContext(ToastContext);
}
