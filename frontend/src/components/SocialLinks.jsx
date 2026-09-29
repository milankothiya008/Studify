import { Globe } from "lucide-react";

// The icon library has no brand logos, so these three are small SVG drawings.
function LinkedInIcon({ size }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
      <path d="M4.98 3.5a2.5 2.5 0 1 1 0 5 2.5 2.5 0 0 1 0-5zM3 9.75h4v11H3v-11zm6.5 0h3.8v1.5h.06c.53-1 1.83-2.05 3.77-2.05 4.03 0 4.77 2.65 4.77 6.1v5.45h-4v-4.83c0-1.15-.02-2.64-1.6-2.64-1.61 0-1.86 1.26-1.86 2.55v4.92h-4v-11z" />
    </svg>
  );
}

function YouTubeIcon({ size }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
      <path d="M23 7.2a3 3 0 0 0-2.1-2.1C19 4.6 12 4.6 12 4.6s-7 0-8.9.5A3 3 0 0 0 1 7.2 31 31 0 0 0 .5 12a31 31 0 0 0 .5 4.8 3 3 0 0 0 2.1 2.1c1.9.5 8.9.5 8.9.5s7 0 8.9-.5a3 3 0 0 0 2.1-2.1c.4-1.6.5-3.2.5-4.8s-.1-3.2-.5-4.8zM9.75 15.02V8.98L15.5 12l-5.75 3.02z" />
    </svg>
  );
}

function XIcon({ size }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
      <path d="M17.75 3h3.07l-6.7 7.66L22 21h-6.17l-4.83-6.32L5.47 21H2.4l7.17-8.2L2 3h6.33l4.37 5.77L17.75 3zm-1.08 16.2h1.7L7.4 4.73H5.58L16.67 19.2z" />
    </svg>
  );
}

// Round icon links to the website, LinkedIn, YouTube and X of a person.
// Only the links that are filled in are shown. "light" is for dark backgrounds.
function SocialLinks({ person, light }) {
  const links = [
    { url: person.websiteUrl, label: "Website", Icon: Globe },
    { url: person.linkedInUrl, label: "LinkedIn", Icon: LinkedInIcon },
    { url: person.youTubeUrl, label: "YouTube", Icon: YouTubeIcon },
    { url: person.twitterUrl, label: "X (Twitter)", Icon: XIcon },
  ].filter((link) => link.url);

  if (links.length === 0) {
    return null;
  }

  return (
    <div className={light ? "social-links social-links-light" : "social-links"}>
      {links.map(function (link, index) {
        const Icon = link.Icon;
        return (
          <a
            key={link.label}
            href={link.url}
            target="_blank"
            rel="noopener noreferrer nofollow"
            title={link.label}
            aria-label={link.label}
            className="social-link"
            style={{ "--i": index }}
          >
            <Icon size={18} />
          </a>
        );
      })}
    </div>
  );
}

export default SocialLinks;
