// A friendly message for empty lists and errors, with an icon.
// Example: <EmptyState icon={BookOpen} title="No courses yet" text="..."><button>...</button></EmptyState>
function EmptyState({ icon, title, text, children }) {
  const Icon = icon; // a component from lucide-react, e.g. BookOpen

  return (
    <div className="empty-state">
      {Icon && (
        <div className="empty-state-icon">
          <Icon size={32} />
        </div>
      )}
      <h2>{title}</h2>
      {text && <p>{text}</p>}
      {children && <div className="empty-state-actions">{children}</div>}
    </div>
  );
}

export default EmptyState;
