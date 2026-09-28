import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { BadgeCheck, CalendarClock, Check, Lock, RefreshCw, Sparkles, Unlock } from "lucide-react";
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

  const isSubscribed = Boolean(subscription && subscription.isActive);

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
        {isSubscribed && (
          <div className="status-card status-active">
            <Unlock size={22} />
            <div>
              <strong>Your current plan: {subscription.planName}</strong>
              <p>
                Active until {formatDate(subscription.currentPlanEndDate)}. You have access to every course for{" "}
                {subscription.daysLeft} more {subscription.daysLeft === 1 ? "day" : "days"}
                {subscription.upcomingPlans.length > 0 && " (until " + formatDate(subscription.endDate) + ")"}.
              </p>
              {subscription.upcomingPlans.map(function (upcoming) {
                return (
                  <p key={upcoming.startDate} className="upcoming-plan">
                    <CalendarClock size={15} /> Next: {upcoming.planName} plan, {formatDate(upcoming.startDate)} to{" "}
                    {formatDate(upcoming.endDate)}
                  </p>
                );
              })}
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
            const isCurrent = isSubscribed && plan.id === subscription.planId;
            // Already bought in advance, starts after the current plan.
            const upcoming = isSubscribed ? subscription.upcomingPlans.find((u) => u.planId === plan.id) : null;
            const isBest = plan.id === bestPlanId && !isCurrent && !upcoming;

            let cardClass = "plan-card stagger-item";
            if (isCurrent) {
              cardClass = cardClass + " current";
            } else if (isBest) {
              cardClass = cardClass + " best";
            }

            return (
              <div key={plan.id} className={cardClass} style={{ "--i": index }}>
                {isCurrent && (
                  <div className="plan-ribbon plan-ribbon-current">
                    <BadgeCheck size={14} /> Your current plan
                  </div>
                )}
                {upcoming && !isCurrent && (
                  <div className="plan-ribbon plan-ribbon-next">
                    <CalendarClock size={14} /> Starts {formatDate(upcoming.startDate)}
                  </div>
                )}
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
                {isCurrent ? (
                  <>
                    <button className="btn btn-block btn-large btn-current" disabled>
                      <BadgeCheck size={18} /> Current plan
                    </button>
                    <p className="plan-note">
                      Active until {formatDate(subscription.currentPlanEndDate)}.{" "}
                      <button className="link-button" onClick={() => choosePlan(plan.id)}>
                        Renew in advance
                      </button>
                    </p>
                  </>
                ) : (
                  <>
                    <button
                      className={isBest ? "btn btn-primary btn-block btn-large" : "btn btn-outline btn-block btn-large"}
                      onClick={() => choosePlan(plan.id)}
                    >
                      Get {plan.name}
                    </button>
                    {isSubscribed && (
                      <p className="plan-note">Starts on {formatDate(subscription.endDate)}, after your current plan.</p>
                    )}
                  </>
                )}
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
