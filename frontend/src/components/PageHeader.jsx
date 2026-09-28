// The dark banner at the top of most pages, so every page starts the same way.
// Example: <PageHeader title="My learning" subtitle="Pick up where you left off" />
// Anything inside it (buttons ...) is shown on the right.
function PageHeader({ title, subtitle, children, center }) {
  return (
    <section className={center ? "page-header page-header-center" : "page-header"}>
      <div className="header-glow header-glow-1"></div>
      <div className="header-glow header-glow-2"></div>
      <div className="container page-header-inner">
        <div className="page-header-text">
          <h1>{title}</h1>
          {subtitle && <p>{subtitle}</p>}
        </div>
        {children && <div className="page-header-actions">{children}</div>}
      </div>
    </section>
  );
}

export default PageHeader;
