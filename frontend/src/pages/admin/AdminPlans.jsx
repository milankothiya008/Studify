import { useEffect, useState } from "react";
import api, { getErrorMessage } from "../../api";
import { formatPrice } from "../../utils";

const EMPTY_FORM = { id: null, name: "", description: "", price: "", durationDays: "", isActive: true };

function AdminPlans() {
  const [plans, setPlans] = useState([]);
  const [form, setForm] = useState(EMPTY_FORM); // form.id = null means "new plan"
  const [error, setError] = useState("");

  function loadPlans() {
    api.get("/plans?all=true").then(function (response) {
      setPlans(response.data);
    });
  }

  useEffect(function () {
    loadPlans();
  }, []);

  // Updates one field of the form.
  function setField(name, value) {
    const copy = { ...form };
    copy[name] = value;
    setForm(copy);
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setError("");

    const body = {
      name: form.name,
      description: form.description,
      price: Number(form.price),
      durationDays: Number(form.durationDays),
      isActive: form.isActive,
    };

    try {
      if (form.id) {
        await api.put("/plans/" + form.id, body);
      } else {
        await api.post("/plans", body);
      }
      setForm(EMPTY_FORM);
      loadPlans();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  }

  async function handleDelete(plan) {
    if (!window.confirm('Delete plan "' + plan.name + '"?')) {
      return;
    }
    try {
      await api.delete("/plans/" + plan.id);
      loadPlans();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  }

  return (
    <div>
      {error && <div className="alert alert-error">{error}</div>}

      <div className="table-wrapper">
        <table className="table">
          <thead>
            <tr>
              <th>Name</th>
              <th>Price</th>
              <th>Days</th>
              <th>Status</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {plans.map(function (plan) {
              return (
                <tr key={plan.id}>
                  <td>
                    <strong>{plan.name}</strong>
                    <div className="muted small">{plan.description}</div>
                  </td>
                  <td>{formatPrice(plan.price)}</td>
                  <td>{plan.durationDays}</td>
                  <td>
                    {plan.isActive ? (
                      <span className="badge badge-success">Active</span>
                    ) : (
                      <span className="badge">Hidden</span>
                    )}
                  </td>
                  <td>
                    <button className="link-button" onClick={() => setForm(plan)}>
                      Edit
                    </button>{" "}
                    ·{" "}
                    <button className="link-button danger" onClick={() => handleDelete(plan)}>
                      Delete
                    </button>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      <form className="box" onSubmit={handleSubmit}>
        <h3>{form.id ? "Edit plan" : "Add a plan"}</h3>

        <div className="form-row">
          <div>
            <label>Name</label>
            <input value={form.name} onChange={(e) => setField("name", e.target.value)} required />
          </div>
          <div>
            <label>Price (₹)</label>
            <input type="number" min="1" value={form.price} onChange={(e) => setField("price", e.target.value)} required />
          </div>
          <div>
            <label>Duration (days)</label>
            <input
              type="number"
              min="1"
              value={form.durationDays}
              onChange={(e) => setField("durationDays", e.target.value)}
              required
            />
          </div>
        </div>

        <label>Description</label>
        <input value={form.description || ""} onChange={(e) => setField("description", e.target.value)} />

        <label className="checkbox-label">
          <input type="checkbox" checked={form.isActive} onChange={(e) => setField("isActive", e.target.checked)} />
          Active (shown on the pricing page)
        </label>

        <div className="card-actions">
          <button type="submit" className="btn btn-primary">
            {form.id ? "Save plan" : "Add plan"}
          </button>
          {form.id && (
            <button type="button" className="btn btn-outline" onClick={() => setForm(EMPTY_FORM)}>
              Cancel
            </button>
          )}
        </div>
      </form>
    </div>
  );
}

export default AdminPlans;
