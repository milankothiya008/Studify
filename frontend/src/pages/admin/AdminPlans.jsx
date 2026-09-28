import { useEffect, useState } from "react";
import { Pencil, Trash2 } from "lucide-react";
import api, { getErrorMessage } from "../../api";
import { useToast } from "../../ToastContext";
import { SkeletonRows } from "../../components/Skeleton";
import { formatPrice } from "../../utils";

const EMPTY_FORM = { id: null, name: "", description: "", price: "", durationDays: "", isActive: true };

function AdminPlans() {
  const showToast = useToast();
  const [plans, setPlans] = useState([]);
  const [loading, setLoading] = useState(true);
  const [form, setForm] = useState(EMPTY_FORM); // form.id = null means "new plan"

  function loadPlans() {
    api
      .get("/plans?all=true")
      .then(function (response) {
        setPlans(response.data);
      })
      .finally(function () {
        setLoading(false);
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

  function startEdit(plan) {
    setForm({
      id: plan.id,
      name: plan.name,
      description: plan.description || "",
      price: plan.price,
      durationDays: plan.durationDays,
      isActive: plan.isActive,
    });
    window.scrollTo({ top: document.body.scrollHeight, behavior: "smooth" });
  }

  async function handleSubmit(event) {
    event.preventDefault();

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
        showToast("Plan saved.");
      } else {
        await api.post("/plans", body);
        showToast("Plan added.");
      }
      setForm(EMPTY_FORM);
      loadPlans();
    } catch (err) {
      showToast(getErrorMessage(err), "error");
    }
  }

  async function handleDelete(plan) {
    if (!window.confirm('Delete plan "' + plan.name + '"?')) {
      return;
    }
    try {
      await api.delete("/plans/" + plan.id);
      showToast("Plan deleted.");
      loadPlans();
    } catch (err) {
      showToast(getErrorMessage(err), "error");
    }
  }

  return (
    <div>
      {loading ? (
        <SkeletonRows rows={3} />
      ) : (
        <div className="card table-card">
          <div className="table-wrapper">
            <table className="table">
              <thead>
                <tr>
                  <th>Plan</th>
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
                        <div className="table-actions">
                          <button className="icon-button" title="Edit" onClick={() => startEdit(plan)}>
                            <Pencil size={16} />
                          </button>
                          <button className="icon-button danger" title="Delete" onClick={() => handleDelete(plan)}>
                            <Trash2 size={16} />
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </div>
      )}

      <form className="card card-padded plan-form" onSubmit={handleSubmit}>
        <h3>{form.id ? "Edit plan" : "Add a plan"}</h3>

        <div className="form-row form-row-3">
          <div className="form-field">
            <label>Name</label>
            <input value={form.name} onChange={(e) => setField("name", e.target.value)} maxLength={100} required />
          </div>
          <div className="form-field">
            <label>Price (₹)</label>
            <input type="number" min="1" value={form.price} onChange={(e) => setField("price", e.target.value)} required />
          </div>
          <div className="form-field">
            <label>Duration (days)</label>
            <input
              type="number"
              min="1"
              max="3650"
              value={form.durationDays}
              onChange={(e) => setField("durationDays", e.target.value)}
              required
            />
          </div>
        </div>

        <div className="form-field">
          <label>Description</label>
          <input value={form.description} onChange={(e) => setField("description", e.target.value)} />
        </div>

        <label className="checkbox-label">
          <input type="checkbox" checked={form.isActive} onChange={(e) => setField("isActive", e.target.checked)} />
          Active (shown on the pricing page)
        </label>

        <div className="card-actions">
          <button type="submit" className="btn btn-primary">
            {form.id ? "Save plan" : "Add plan"}
          </button>
          {form.id && (
            <button type="button" className="btn btn-ghost" onClick={() => setForm(EMPTY_FORM)}>
              Cancel
            </button>
          )}
        </div>
      </form>
    </div>
  );
}

export default AdminPlans;
