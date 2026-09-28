import { useState } from "react";
import { AlertCircle } from "lucide-react";
import api, { getErrorMessage } from "../../api";
import { useToast } from "../../ToastContext";

const LEVELS = ["Beginner", "Intermediate", "Advanced", "All Levels"];

// The "Course details" tab: title, description, price and so on.
function CourseDetailsForm({ course, categories, onSaved }) {
  const showToast = useToast();
  const [title, setTitle] = useState(course.title || "");
  const [subtitle, setSubtitle] = useState(course.subtitle || "");
  const [description, setDescription] = useState(course.description || "");
  const [whatYouWillLearn, setWhatYouWillLearn] = useState(course.whatYouWillLearn || "");
  const [requirements, setRequirements] = useState(course.requirements || "");
  const [categoryId, setCategoryId] = useState(course.categoryId || "");
  const [level, setLevel] = useState(course.level || "All Levels");
  const [language, setLanguage] = useState(course.language || "English");
  const [price, setPrice] = useState(course.price);
  const [error, setError] = useState("");
  const [saving, setSaving] = useState(false);

  async function handleSubmit(event) {
    event.preventDefault();
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
      showToast("Course details saved.");
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

      <div className="form-field">
        <label htmlFor="title">Course title</label>
        <input id="title" value={title} onChange={(e) => setTitle(e.target.value)} maxLength={200} required />
        <p className="field-hint">{title.length} / 200</p>
      </div>

      <div className="form-field">
        <label htmlFor="subtitle">Subtitle</label>
        <input
          id="subtitle"
          value={subtitle}
          placeholder="One sentence about what students get"
          onChange={(e) => setSubtitle(e.target.value)}
          maxLength={300}
        />
      </div>

      <div className="form-field">
        <label htmlFor="description">Description</label>
        <textarea id="description" rows={6} value={description} onChange={(e) => setDescription(e.target.value)} />
      </div>

      <div className="form-row">
        <div className="form-field">
          <label htmlFor="learn">What will students learn?</label>
          <textarea
            id="learn"
            rows={5}
            value={whatYouWillLearn}
            placeholder={"Build a website\nUse React hooks"}
            onChange={(e) => setWhatYouWillLearn(e.target.value)}
          />
          <p className="field-hint">One item per line.</p>
        </div>
        <div className="form-field">
          <label htmlFor="requirements">Requirements</label>
          <textarea
            id="requirements"
            rows={5}
            value={requirements}
            placeholder={"A computer with internet\nNo experience needed"}
            onChange={(e) => setRequirements(e.target.value)}
          />
          <p className="field-hint">One item per line.</p>
        </div>
      </div>

      <div className="form-row form-row-4">
        <div className="form-field">
          <label htmlFor="category">Category</label>
          <select id="category" value={categoryId} onChange={(e) => setCategoryId(e.target.value)}>
            <option value="">Choose a category</option>
            {categories.map(function (category) {
              return (
                <option key={category.id} value={category.id}>
                  {category.name}
                </option>
              );
            })}
          </select>
        </div>
        <div className="form-field">
          <label htmlFor="level">Level</label>
          <select id="level" value={level} onChange={(e) => setLevel(e.target.value)}>
            {LEVELS.map(function (item) {
              return (
                <option key={item} value={item}>
                  {item}
                </option>
              );
            })}
          </select>
        </div>
        <div className="form-field">
          <label htmlFor="language">Language</label>
          <input id="language" value={language} onChange={(e) => setLanguage(e.target.value)} />
        </div>
        <div className="form-field">
          <label htmlFor="price">Price (₹)</label>
          <input
            id="price"
            type="number"
            min="0"
            step="1"
            value={price}
            onChange={(e) => setPrice(e.target.value)}
            required
          />
          <p className="field-hint">0 = free course</p>
        </div>
      </div>

      {error && (
        <div className="alert alert-error">
          <AlertCircle size={18} /> {error}
        </div>
      )}

      <div className="form-actions">
        <button type="submit" className="btn btn-primary" disabled={saving}>
          {saving && <span className="btn-spinner"></span>}
          {saving ? "Saving..." : "Save details"}
        </button>
      </div>
    </form>
  );
}

export default CourseDetailsForm;
