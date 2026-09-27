// A simple loading indicator.
function Spinner({ text }) {
  return (
    <div className="spinner-wrapper">
      <div className="spinner"></div>
      <p>{text || "Loading..."}</p>
    </div>
  );
}

export default Spinner;
