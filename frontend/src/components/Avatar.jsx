import { getInitials } from "../utils";

// Round profile picture. Shows the initials when there is no photo.
function Avatar({ name, imageUrl, size }) {
  const style = { width: size, height: size, fontSize: size * 0.4 };

  if (imageUrl) {
    return <img className="avatar" src={imageUrl} alt={name} style={style} />;
  }

  return (
    <div className="avatar avatar-initials" style={style}>
      {getInitials(name)}
    </div>
  );
}

export default Avatar;
