import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import api, { getErrorMessage } from "../api";
import Spinner from "../components/Spinner";
import { formatMoney, formatPrice } from "../utils";

// DEMO checkout page. Clicking "Pay" does not take real money.
// URL: /checkout/course/5  or  /checkout/plan/2
function CheckoutPage() {
  const { type, id } = useParams();

  const [item, setItem] = useState(null); // { title, description, price, imageUrl }
  const [error, setError] = useState("");
  const [paying, setPaying] = useState(false);
  const [result, setResult] = useState(null); // answer from the API after paying

  useEffect(
    function () {
      async function loadItem() {
        try {
          if (type === "course") {
            const response = await api.get("/courses/" + id);
            const course = response.data;
            setItem({
              title: course.title,
              description: "Lifetime access to this course · by " + course.instructorName,
              price: course.price,
              imageUrl: course.thumbnailUrl,
            });
          } else {
            const response = await api.get("/plans");
            const plan = response.data.find((p) => String(p.id) === id);
            if (!plan) {
              setError("Plan not found.");
              return;
            }
            setItem({
              title: plan.name + " subscription",
              description: "Access every course for " + plan.durationDays + " days",
              price: plan.price,
              imageUrl: null,
            });
          }
        } catch (err) {
          setError(getErrorMessage(err));
        }
      }
      loadItem();
    },
    [type, id]
  );

  async function handlePay() {
    setPaying(true);
    setError("");
    try {
      const response = await api.post("/checkout/" + type + "/" + id);
      setResult(response.data);
    } catch (err) {
      setError(getErrorMessage(err));
    }
    setPaying(false);
  }

  // After a successful payment
  if (result) {
    return (
      <div className="container page">
        <div className="success-card">
          <div className="success-icon">✓</div>
          <h1>Thank you!</h1>
          <p>{result.message}</p>
          <p className="muted small">
            Amount: {formatMoney(result.amount)} · Transaction: {result.transactionId}
          </p>
          <div className="card-actions center-actions">
            {type === "course" ? (
              <Link to={"/learn/" + id} className="btn btn-primary">
                Start learning
              </Link>
            ) : (
              <Link to="/courses" className="btn btn-primary">
                Browse all courses
              </Link>
            )}
            <Link to="/purchases" className="btn btn-outline">
              Purchase history
            </Link>
          </div>
        </div>
      </div>
    );
  }

  if (!item && !error) {
    return <Spinner />;
  }

  return (
    <div className="container page">
      <h1>Checkout</h1>

      {error && <div className="alert alert-error">{error}</div>}

      {item && (
        <div className="checkout-layout">
          <div>
            <h2>Order details</h2>
            <div className="order-item">
              {item.imageUrl ? <img src={item.imageUrl} alt="" /> : <div className="order-icon">⭐</div>}
              <div>
                <strong>{item.title}</strong>
                <p className="muted small">{item.description}</p>
              </div>
              <strong>{formatPrice(item.price)}</strong>
            </div>

            <h2>Payment method</h2>
            <div className="alert alert-info">
              <strong>Demo mode.</strong> This project does not take real payments. Click the button to simulate a
              successful payment. (See CheckoutController.cs to connect Razorpay or Stripe later.)
            </div>
          </div>

          <div className="summary-card">
            <h3>Summary</h3>
            <div className="summary-row">
              <span>Original price</span>
              <span>{formatMoney(item.price)}</span>
            </div>
            <div className="summary-row total">
              <span>Total</span>
              <span>{formatMoney(item.price)}</span>
            </div>
            <button className="btn btn-primary btn-block btn-large" onClick={handlePay} disabled={paying}>
              {paying ? "Processing..." : "Pay " + formatMoney(item.price)}
            </button>
            <p className="muted small center">No real money will be charged.</p>
          </div>
        </div>
      )}
    </div>
  );
}

export default CheckoutPage;
