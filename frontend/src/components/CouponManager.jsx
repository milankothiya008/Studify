import { useEffect, useState } from "react";
import { Copy, Plus, Tag, Trash2 } from "lucide-react";
import api, { getErrorMessage } from "../api";
import { useToast } from "../ToastContext";
import { SkeletonRows } from "./Skeleton";
import { formatDate } from "../utils";

// Create and manage discount codes.
//   Instructor: <CouponManager courseId={5} />        -> codes for that course only
//   Admin:      <CouponManager isAdmin courses={...} /> -> codes for all courses, plans, or one course
function CouponManager({ courseId, isAdmin, courses }) {
  const showToast = useToast();
  const [coupons, setCoupons] = useState([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);

  // Form fields
  const [code, setCode] = useState("");
  const [discountPercent, setDiscountPercent] = useState(20);
  const [appliesTo, setAppliesTo] = useState("all"); // admin only: "all", "plans" or a course id
  const [expiresOn, setExpiresOn] = useState(""); // "2026-12-31" or empty
  const [maxUses, setMaxUses] = useState("");
  const [saving, setSaving] = useState(false);

  function load() {
    api
      .get("/coupons", { params: { courseId: courseId || null } })
      .then(function (response) {
        setCoupons(response.data);
      })
      .finally(function () {
        setLoading(false);
      });
  }

  useEffect(
    function () {
      load();
    },
    [courseId]
  );

  function resetForm() {
    setCode("");
    setDiscountPercent(20);
    setAppliesTo("all");
    setExpiresOn("");
    setMaxUses("");
  }

  async function handleCreate(event) {
    event.preventDefault();

    // Which items the coupon is for.
    let couponCourseId = courseId || null;
    let forPlans = false;
    if (isAdmin && !courseId) {
      if (appliesTo === "plans") {
        forPlans = true;
      } else if (appliesTo !== "all") {
        couponCourseId = Number(appliesTo);
      }
    }

    setSaving(true);
    try {
      const response = await api.post("/coupons", {
        code: code,
        discountPercent: Number(discountPercent),
        courseId: couponCourseId,
        forPlans: forPlans,
        // The coupon works until the end of the chosen day.
        expiresAt: expiresOn ? new Date(expiresOn + "T23:59:59").toISOString() : null,
        maxUses: maxUses ? Number(maxUses) : null,
        isActive: true,
      });
      showToast(response.data.message);
      resetForm();
      setShowForm(false);
      load();
    } catch (err) {
      showToast(getErrorMessage(err), "error");
    }
    setSaving(false);
  }

  async function toggleActive(coupon) {
    try {
      await api.put("/coupons/" + coupon.id, {
        code: coupon.code,
        discountPercent: coupon.discountPercent,
        courseId: coupon.courseId,
        forPlans: coupon.forPlans,
        expiresAt: coupon.expiresAt,
        maxUses: coupon.maxUses,
        isActive: !coupon.isActive,
      });
      showToast(coupon.isActive ? "Coupon turned off." : "Coupon turned on.", "info");
      load();
    } catch (err) {
      showToast(getErrorMessage(err), "error");
    }
  }

  async function handleDelete(coupon) {
    if (!window.confirm("Delete the coupon " + coupon.code + "?")) {
      return;
    }
    try {
      await api.delete("/coupons/" + coupon.id);
      showToast("Coupon deleted.");
      load();
    } catch (err) {
      showToast(getErrorMessage(err), "error");
    }
  }

  async function copyCode(couponCode) {
    try {
      await navigator.clipboard.writeText(couponCode);
      showToast("Copied " + couponCode + ".");
    } catch (err) {
      // copying is only a convenience
    }
  }

  function appliesToText(coupon) {
    if (coupon.forPlans) {
      return "Subscription plans";
    }
    if (coupon.courseTitle) {
      return coupon.courseTitle;
    }
    return "All courses";
  }

  function isExpired(coupon) {
    return coupon.expiresAt && new Date(coupon.expiresAt) < new Date();
  }

  // Tomorrow, as the earliest expiry date the date picker allows.
  const tomorrow = new Date(Date.now() + 86400000).toISOString().slice(0, 10);

  return (
    <div className="coupon-manager">
      <div className="table-toolbar">
        <p className="muted small">
          Students type the code at checkout to get the discount.
          {!isAdmin && " Share it on social media, in emails or with your students."}
        </p>
        <button className="btn btn-primary btn-small" onClick={() => setShowForm(!showForm)}>
          <Plus size={16} /> New coupon
        </button>
      </div>

      {showForm && (
        <form className="card card-padded coupon-create" onSubmit={handleCreate}>
          <div className="form-row form-row-4">
            <div className="form-field">
              <label htmlFor="couponCode">Code</label>
              <input
                id="couponCode"
                value={code}
                onChange={(e) => setCode(e.target.value.toUpperCase().replace(/\s/g, ""))}
                placeholder="e.g. LAUNCH50"
                pattern="[A-Za-z0-9_\-]{3,30}"
                title="3-30 letters, numbers, - or _"
                required
              />
            </div>
            <div className="form-field">
              <label htmlFor="couponPercent">Discount (%)</label>
              <input
                id="couponPercent"
                type="number"
                min="1"
                max="100"
                value={discountPercent}
                onChange={(e) => setDiscountPercent(e.target.value)}
                required
              />
            </div>
            <div className="form-field">
              <label htmlFor="couponExpires">Expires on (optional)</label>
              <input id="couponExpires" type="date" min={tomorrow} value={expiresOn} onChange={(e) => setExpiresOn(e.target.value)} />
            </div>
            <div className="form-field">
              <label htmlFor="couponMax">Max uses (optional)</label>
              <input
                id="couponMax"
                type="number"
                min="1"
                value={maxUses}
                onChange={(e) => setMaxUses(e.target.value)}
                placeholder="Unlimited"
              />
            </div>
          </div>

          {isAdmin && !courseId && (
            <div className="form-field">
              <label htmlFor="couponScope">Works for</label>
              <select id="couponScope" value={appliesTo} onChange={(e) => setAppliesTo(e.target.value)}>
                <option value="all">All courses</option>
                <option value="plans">Subscription plans</option>
                {(courses || []).map(function (course) {
                  return (
                    <option key={course.id} value={course.id}>
                      Only: {course.title}
                    </option>
                  );
                })}
              </select>
            </div>
          )}

          <div className="card-actions">
            <button type="submit" className="btn btn-primary" disabled={saving}>
              Create coupon
            </button>
            <button type="button" className="btn btn-ghost" onClick={() => setShowForm(false)}>
              Cancel
            </button>
          </div>
        </form>
      )}

      {loading && <SkeletonRows rows={3} />}

      {!loading && coupons.length === 0 && (
        <div className="qa-empty">
          <Tag size={28} />
          <p>No coupons yet. Create one to run a promotion.</p>
        </div>
      )}

      {!loading && coupons.length > 0 && (
        <div className="card table-card">
          <div className="table-wrapper">
            <table className="table">
              <thead>
                <tr>
                  <th>Code</th>
                  <th>Discount</th>
                  {!courseId && <th>Works for</th>}
                  <th>Used</th>
                  <th>Expires</th>
                  <th>Status</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {coupons.map(function (coupon) {
                  return (
                    <tr key={coupon.id}>
                      <td>
                        <button className="coupon-code" onClick={() => copyCode(coupon.code)} title="Copy code">
                          {coupon.code} <Copy size={13} />
                        </button>
                      </td>
                      <td>
                        <strong>{coupon.discountPercent}%</strong>
                      </td>
                      {!courseId && <td>{appliesToText(coupon)}</td>}
                      <td>
                        {coupon.usedCount}
                        {coupon.maxUses ? " / " + coupon.maxUses : ""}
                      </td>
                      <td>{coupon.expiresAt ? formatDate(coupon.expiresAt) : "Never"}</td>
                      <td>
                        {isExpired(coupon) ? (
                          <span className="badge">Expired</span>
                        ) : (
                          <label className="switch" title={coupon.isActive ? "Turn off" : "Turn on"}>
                            <input type="checkbox" checked={coupon.isActive} onChange={() => toggleActive(coupon)} />
                            <span className="switch-slider"></span>
                          </label>
                        )}
                      </td>
                      <td>
                        <div className="table-actions">
                          <button className="icon-button danger" title="Delete" onClick={() => handleDelete(coupon)}>
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
    </div>
  );
}

export default CouponManager;
