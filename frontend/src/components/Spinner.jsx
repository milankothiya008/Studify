// A loading indicator for a whole page.
function Spinner({ text }) {
  return (
    <div className="spinner-wrapper">
      <div className="spinner"></div>
      <p>{text || "Loading..."}</p>
    </div>
  );
}

export default Spinner;
