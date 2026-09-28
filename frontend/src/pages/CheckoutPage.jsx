import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { AlertCircle, CheckCircle2, CreditCard, Lock, Mail, ShieldCheck, Sparkles, Tag, X } from "lucide-react";
import api, { getErrorMessage } from "../api";
import PageHeader from "../components/PageHeader";
import Spinner from "../components/Spinner";
import EmptyState from "../components/EmptyState";
import { formatMoney, formatPrice } from "../utils";

// DEMO checkout page. Clicking "Pay" does not take real money.
// URL: /checkout/course/5  or  /checkout/plan/2
function CheckoutPage() {
  const { type, id } = useParams();

  const [item, setItem] = useState(null); // { title, description, price, imageUrl, alreadyOwned }
  const [loadError, setLoadError] = useState("");
  const [payError, setPayError] = useState("");
  const [paying, setPaying] = useState(false);
  const [result, setResult] = useState(null); // answer from the API after paying

  // Coupon code
  const [couponInput, setCouponInput] = useState("");
  const [price, setPrice] = useState(null); // { originalAmount, discountAmount, finalAmount, couponCode, ... }
  const [couponError, setCouponError] = useState("");
  const [checkingCoupon, setCheckingCoupon] = useState(false);

  useEffect(
    function () {
      async function loadItem() {
        try {
          if (type === "course") {
            const response = await api.get("/courses/" + id);
            const course = response.data;
            setItem({
              title: course.title,
              description: "Lifetime access · by " + course.instructorName,
              price: course.price,
              imageUrl: course.thumbnailUrl,
              // Owners, buyers and free courses don't need to pay.
              alreadyOwned: course.isOwner || course.hasPurchased || Number(course.price) === 0,
            });
          } else if (type === "plan") {
            const response = await api.get("/plans");
            const plan = response.data.find((p) => String(p.id) === id);
            if (!plan) {
              setLoadError("This plan is not available anymore.");
              return;
            }
            setItem({
              title: plan.name + " subscription",
              description: "Access every course for " + plan.durationDays + " days",
              price: plan.price,
              imageUrl: null,
              alreadyOwned: false,
            });
          } else {
            setLoadError("Nothing to buy here.");
          }
        } catch (err) {
          setLoadError(getErrorMessage(err));
        }
      }
      loadItem();
    },
    [type, id]
  );

  // Asks the server for the price with (or without) a coupon code.
  async function loadPrice(code) {
    const response = await api.post("/checkout/preview", { itemType: type, itemId: Number(id), couponCode: code });
    setPrice(response.data);
    return response.data;
  }

  // The price without a coupon, as soon as the item is known.
  useEffect(
    function () {
      if (item && !item.alreadyOwned) {
        loadPrice("").catch(function () {});
      }
    },
    [item]
  );

  async function applyCoupon(event) {
    event.preventDefault();
    setCouponError("");
    setCheckingCoupon(true);
    try {
      const preview = await loadPrice(couponInput);
      if (preview.couponError) {
        setCouponError(preview.couponError);
      }
    } catch (err) {
      setCouponError(getErrorMessage(err));
    }
    setCheckingCoupon(false);
  }

  function removeCoupon() {
    setCouponInput("");
    setCouponError("");
    loadPrice("").catch(function () {});
  }

  async function handlePay() {
    setPaying(true);
    setPayError("");
    try {
      const couponCode = price && price.couponApplied ? price.couponCode : "";
      const response = await api.post("/checkout/" + type + "/" + id, { couponCode: couponCode });
      setResult(response.data);
    } catch (err) {
      setPayError(getErrorMessage(err));
    }
    setPaying(false);
  }

  // ---------- After a successful payment ----------
  if (result) {
    return (
      <div className="container page">
        <div className="success-card card card-padded animate-in">
          <div className="success-icon">
            <CheckCircle2 size={44} />
          </div>
          <h1>Thank you!</h1>
          <p>{result.message}</p>
          <div className="receipt">
            <div>
              <span>Amount paid</span>
              <strong>{formatMoney(result.amount)}</strong>
              {result.discountAmount > 0 && <small className="saved-text">You saved {formatMoney(result.discountAmount)}</small>}
            </div>
            <div>
              <span>Transaction</span>
              <strong>{result.transactionId}</strong>
            </div>
          </div>
          <p className="muted small">
            <Mail size={14} /> A confirmation email is on its way.
          </p>
          <div className="card-actions center-actions">
            {type === "course" ? (
              <Link to={"/learn/" + id} className="btn btn-primary btn-large">
                Start learning
              </Link>
            ) : (
              <Link to="/courses" className="btn btn-primary btn-large">
                Browse all courses
              </Link>
            )}
            <Link to="/purchases" className="btn btn-outline btn-large">
              Purchase history
            </Link>
          </div>
        </div>
      </div>
    );
  }

  if (loadError) {
    return (
      <div className="container page">
        <EmptyState icon={AlertCircle} title={loadError}>
          <Link to="/courses" className="btn btn-primary">
            Browse courses
          </Link>
        </EmptyState>
      </div>
    );
  }

  if (!item) {
    return <Spinner />;
  }

  if (item.alreadyOwned) {
    return (
      <div className="container page">
        <EmptyState icon={CheckCircle2} title="You already have access" text="There is nothing to pay for this course.">
          <Link to={"/course/" + id} className="btn btn-primary">
            Go to the course
          </Link>
        </EmptyState>
      </div>
    );
  }

  return (
    <div>
      <PageHeader title="Checkout" subtitle="Review your order and complete the payment." />

      <div className="container page">
        <div className="checkout-layout">
          <div>
            <h2 className="step-title">
              <span className="step-number">1</span> Order details
            </h2>
            <div className="order-item card">
              {item.imageUrl ? (
                <img src={item.imageUrl} alt="" />
              ) : (
                <div className="order-icon">
                  <Sparkles size={28} />
                </div>
              )}
              <div>
                <strong>{item.title}</strong>
                <p className="muted small">{item.description}</p>
              </div>
              <strong className="order-price">{formatPrice(item.price)}</strong>
            </div>

            <h2 className="step-title">
              <span className="step-number">2</span> Payment method
            </h2>
            <label className="payment-option card selected">
              <input type="radio" checked readOnly />
              <CreditCard size={22} />
              <div>
                <strong>Demo payment</strong>
                <p className="muted small">
                  No real money is taken. Clicking Pay simulates a successful payment. (See CheckoutController.cs to
                  connect Razorpay or Stripe.)
                </p>
              </div>
            </label>
          </div>

          <div className="summary-card card">
            <h3>Summary</h3>
            <div className="summary-row">
              <span>Price</span>
              <span>{formatMoney(item.price)}</span>
            </div>
            {price && price.couponApplied && (
              <div className="summary-row discount">
                <span>
                  <Tag size={14} /> {price.couponCode} ({price.discountPercent}% off)
                </span>
                <span>-{formatMoney(price.discountAmount)}</span>
              </div>
            )}
            <div className="summary-row total">
              <span>Total</span>
              <span>{formatMoney(price ? price.finalAmount : item.price)}</span>
            </div>

            {/* Coupon code */}
            {price && price.couponApplied ? (
              <div className="coupon-applied">
                <Tag size={16} />
                <span>
                  <strong>{price.couponCode}</strong> applied
                </span>
                <button className="icon-button" onClick={removeCoupon} aria-label="Remove coupon">
                  <X size={16} />
                </button>
              </div>
            ) : (
              <form className="coupon-form" onSubmit={applyCoupon}>
                <input
                  placeholder="Coupon code"
                  value={couponInput}
                  onChange={(e) => setCouponInput(e.target.value.toUpperCase())}
                  maxLength={40}
                />
                <button type="submit" className="btn btn-outline" disabled={!couponInput.trim() || checkingCoupon}>
                  Apply
                </button>
              </form>
            )}
            {couponError && <p className="coupon-error">{couponError}</p>}

            {payError && (
              <div className="alert alert-error">
                <AlertCircle size={18} /> {payError}
              </div>
            )}

            <button className="btn btn-primary btn-block btn-large" onClick={handlePay} disabled={paying}>
              {paying ? <span className="btn-spinner"></span> : <Lock size={18} />}
              {paying ? "Processing..." : "Pay " + formatMoney(price ? price.finalAmount : item.price)}
            </button>
            <p className="secure-note">
              <ShieldCheck size={16} /> Demo checkout · no card details needed
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}

export default CheckoutPage;
