import { useState } from "react";
import api, { getErrorMessage } from "../../api";

const LEVELS = ["Beginner", "Intermediate", "Advanced", "All Levels"];

// The "Course details" tab: title, description, price and so on.
function CourseDetailsForm({ course, categories, onSaved }) {
  const [title, setTitle] = useState(course.title || "");
  const [subtitle, setSubtitle] = useState(course.subtitle || "");
  const [description, setDescription] = useState(course.description || "");
  const [whatYouWillLearn, setWhatYouWillLearn] = useState(course.whatYouWillLearn || "");
  const [requirements, setRequirements] = useState(course.requirements || "");
  const [categoryId, setCategoryId] = useState(course.categoryId || "");
  const [level, setLevel] = useState(course.level || "All Levels");
  const [language, setLanguage] = useState(course.language || "English");
  const [price, setPrice] = useState(course.price);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const [saving, setSaving] = useState(false);

  async function handleSubmit(event) {
    event.preventDefault();
    setMessage("");
    setError("");
    setSaving(true);
    try {
      await api.put("/instructor/courses/" + course.id, {
        title: title,
        subtitle: subtitle,
        description: description,
        whatYouWillLearn: whatYouWillLearn,
        requirements: requirements,
        categoryId: categoryId ? Number(categoryId) : null,
        level: level,
        language: language,
        price: Number(price),
      });
      setMessage("Saved!");
      onSaved();
    } catch (err) {
      setError(getErrorMessage(err));
    }
    setSaving(false);
  }

  return (
    <form onSubmit={handleSubmit}>
      <h2>Course details</h2>
      <p className="muted">This is what students see on your course page.</p>

      <label>Course title</label>
      <input value={title} onChange={(e) => setTitle(e.target.value)} maxLength={200} required />

      <label>Subtitle</label>
      <input
        value={subtitle}
        placeholder="One sentence about what students get"
        onChange={(e) => setSubtitle(e.target.value)}
        maxLength={300}
      />

      <label>Description</label>
      <textarea rows={6} value={description} onChange={(e) => setDescription(e.target.value)} />

      <label>What will students learn? (one per line)</label>
      <textarea
        rows={4}
        value={whatYouWillLearn}
        placeholder={"Build a website\nUse React hooks"}
        onChange={(e) => setWhatYouWillLearn(e.target.value)}
      />

      <label>Requirements (one per line)</label>
      <textarea
        rows={3}
        value={requirements}
        placeholder={"A computer with internet\nNo experience needed"}
        onChange={(e) => setRequirements(e.target.value)}
      />

      <div className="form-row">
        <div>
          <label>Category</label>
          <select value={categoryId} onChange={(e) => setCategoryId(e.target.value)}>
            <option value="">-- Choose a category --</option>
            {categories.map(function (category) {
              return (
                <option key={category.id} value={category.id}>
                  {category.name}
                </option>
              );
            })}
          </select>
        </div>
        <div>
          <label>Level</label>
          <select value={level} onChange={(e) => setLevel(e.target.value)}>
            {LEVELS.map(function (item) {
              return (
                <option key={item} value={item}>
                  {item}
                </option>
              );
            })}
          </select>
        </div>
      </div>

      <div className="form-row">
        <div>
          <label>Language</label>
          <input value={language} onChange={(e) => setLanguage(e.target.value)} />
        </div>
        <div>
          <label>Price (₹) - use 0 for a free course</label>
          <input type="number" min="0" step="1" value={price} onChange={(e) => setPrice(e.target.value)} required />
        </div>
      </div>

      {error && <div className="alert alert-error">{error}</div>}
      {message && <div className="alert alert-success">{message}</div>}

      <button type="submit" className="btn btn-primary" disabled={saving}>
        {saving ? "Saving..." : "Save"}
      </button>
    </form>
  );
}

export default CourseDetailsForm;
