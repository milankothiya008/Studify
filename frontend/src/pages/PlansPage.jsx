import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { CalendarClock, Check, Lock, RefreshCw, Sparkles, Unlock } from "lucide-react";
import api from "../api";
import { useAuth } from "../AuthContext";
import PageHeader from "../components/PageHeader";
import { SkeletonBlock } from "../components/Skeleton";
import { formatDate, formatPrice } from "../utils";

const FEATURES = ["Access to every course", "New courses included", "Progress tracking", "Certificates of completion"];

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

  // The most expensive plan is usually the best value per day.
  let bestPlanId = null;
  if (plans.length > 1) {
    bestPlanId = plans[plans.length - 1].id;
  }

  return (
    <div>
      <PageHeader
        center
        title="One subscription. Every course."
        subtitle="Learn as much as you want while your plan is active. Cancel anytime: it simply ends."
      />

      <div className="container page">
        {subscription && subscription.isActive && (
          <div className="status-card status-active">
            <Unlock size={22} />
            <div>
              <strong>Your {subscription.planName} subscription is active</strong>
              <p>
                Valid until {formatDate(subscription.endDate)} ({subscription.daysLeft} days left). Buying another plan
                adds its days to the end of your current one.
              </p>
            </div>
          </div>
        )}

        {subscription && !subscription.isActive && (
          <div className="status-card">
            <Lock size={22} />
            <div>
              <strong>You don't have an active subscription</strong>
              <p>Choose a plan below to unlock every course.</p>
            </div>
          </div>
        )}

        <div className="plan-grid">
          {loading && (
            <>
              <SkeletonBlock height={420} />
              <SkeletonBlock height={420} />
              <SkeletonBlock height={420} />
            </>
          )}

          {plans.map(function (plan, index) {
            const perDay = plan.price / plan.durationDays;
            const isBest = plan.id === bestPlanId;
            return (
              <div key={plan.id} className={isBest ? "plan-card best stagger-item" : "plan-card stagger-item"} style={{ "--i": index }}>
                {isBest && (
                  <div className="plan-ribbon">
                    <Sparkles size={14} /> Best value
                  </div>
                )}
                <h2>{plan.name}</h2>
                <p className="muted">{plan.description}</p>
                <div className="plan-price">
                  {formatPrice(plan.price)}
                  <span>/ {plan.durationDays} days</span>
                </div>
                <p className="plan-per-day">About {formatPrice(Math.ceil(perDay))} per day</p>
                <ul className="check-list">
                  {FEATURES.map(function (feature) {
                    return (
                      <li key={feature}>
                        <Check size={18} /> {feature}
                      </li>
                    );
                  })}
                </ul>
                <button
                  className={isBest ? "btn btn-primary btn-block btn-large" : "btn btn-outline btn-block btn-large"}
                  onClick={() => choosePlan(plan.id)}
                >
                  {subscription && subscription.isActive ? "Extend with " + plan.name : "Get " + plan.name}
                </button>
              </div>
            );
          })}
        </div>

        <div className="info-grid">
          <div className="info-item">
            <Unlock size={22} />
            <div>
              <strong>Everything unlocked</strong>
              <p>Enroll in any course while your plan is active.</p>
            </div>
          </div>
          <div className="info-item">
            <CalendarClock size={22} />
            <div>
              <strong>When it ends</strong>
              <p>Courses joined through the plan lock until you renew. Your progress is kept.</p>
            </div>
          </div>
          <div className="info-item">
            <RefreshCw size={22} />
            <div>
              <strong>Bought courses stay</strong>
              <p>Courses you bought one by one are yours forever.</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

export default PlansPage;
