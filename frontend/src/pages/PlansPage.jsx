import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import api from "../api";
import { useAuth } from "../AuthContext";
import Spinner from "../components/Spinner";
import { formatDate, formatPrice } from "../utils";

// Subscription plans: one payment unlocks every course for some days.
function PlansPage() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [plans, setPlans] = useState([]);
  const [subscription, setSubscription] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(
    function () {
      async function loadData() {
        try {
          const plansResponse = await api.get("/plans");
          setPlans(plansResponse.data);

          if (user) {
            const subscriptionResponse = await api.get("/account/subscription");
            setSubscription(subscriptionResponse.data);
          }
        } catch (error) {
          console.log(error);
        }
        setLoading(false);
      }
      loadData();
    },
    [user]
  );

  function choosePlan(planId) {
    if (!user) {
      navigate("/login", { state: { from: "/checkout/plan/" + planId } });
      return;
    }
    navigate("/checkout/plan/" + planId);
  }

  if (loading) {
    return <Spinner />;
  }

  // The most expensive plan is usually the best value per day.
  let bestPlanId = null;
  if (plans.length > 1) {
    bestPlanId = plans[plans.length - 1].id;
  }

  return (
    <div className="container page">
      <div className="center">
        <h1>Personal Plan</h1>
        <p className="lead">One subscription. Every course. Learn as much as you want while your plan is active.</p>
      </div>

      {subscription && subscription.isActive && (
        <div className="alert alert-success center">
          ✓ Your <strong>{subscription.planName}</strong> subscription is active until{" "}
          <strong>{formatDate(subscription.endDate)}</strong> ({subscription.daysLeft} days left). Buying another plan
          adds the days to the end of your current one.
        </div>
      )}

      {subscription && !subscription.isActive && (
        <div className="alert alert-warning center">You don't have an active subscription.</div>
      )}

      <div className="plan-grid">
        {plans.map(function (plan) {
          const perDay = plan.price / plan.durationDays;
          return (
            <div key={plan.id} className={plan.id === bestPlanId ? "plan-card best" : "plan-card"}>
              {plan.id === bestPlanId && <div className="plan-ribbon">Best value</div>}
              <h2>{plan.name}</h2>
              <p className="muted">{plan.description}</p>
              <div className="plan-price">{formatPrice(plan.price)}</div>
              <p className="muted small">
                for {plan.durationDays} days · about {formatPrice(Math.ceil(perDay))} per day
              </p>
              <ul className="check-list">
                <li>Access to every course</li>
                <li>New courses included</li>
                <li>Track your progress</li>
                <li>Certificates of completion</li>
              </ul>
              <button className="btn btn-primary btn-block" onClick={() => choosePlan(plan.id)}>
                {subscription && subscription.isActive ? "Extend with " + plan.name : "Subscribe"}
              </button>
            </div>
          );
        })}
      </div>

      <p className="center muted small">
        When a subscription ends, courses you joined through it are locked until you renew. Courses you bought one by one
        are yours forever.
      </p>
    </div>
  );
}

export default PlansPage;
