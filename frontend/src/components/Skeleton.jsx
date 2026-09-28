// Grey shimmering boxes shown while data is loading.

export function SkeletonBlock({ height, width, round }) {
  const style = { height: height || 16, width: width || "100%" };
  return <div className={round ? "skeleton skeleton-round" : "skeleton"} style={style}></div>;
}

export function CourseCardSkeleton() {
  return (
    <div className="course-card course-card-skeleton">
      <div className="skeleton course-card-image"></div>
      <div className="course-card-body">
        <SkeletonBlock height={18} />
        <SkeletonBlock height={18} width="70%" />
        <SkeletonBlock height={12} width="40%" />
        <SkeletonBlock height={12} width="55%" />
        <SkeletonBlock height={20} width="30%" />
      </div>
    </div>
  );
}

// A grid of course card skeletons. count = how many cards.
export function CourseGridSkeleton({ count }) {
  const cards = [];
  for (let i = 0; i < (count || 4); i++) {
    cards.push(<CourseCardSkeleton key={i} />);
  }
  return <div className="course-grid">{cards}</div>;
}

// A few lines of text-like skeletons, for tables and forms.
export function SkeletonRows({ rows }) {
  const lines = [];
  for (let i = 0; i < (rows || 4); i++) {
    lines.push(<SkeletonBlock key={i} height={44} />);
  }
  return <div className="skeleton-rows">{lines}</div>;
}
