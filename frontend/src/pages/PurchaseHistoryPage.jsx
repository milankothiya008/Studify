import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { AlertCircle, Lock, Receipt, Unlock } from "lucide-react";
import api from "../api";
import PageHeader from "../components/PageHeader";
import EmptyState from "../components/EmptyState";
import { SkeletonRows } from "../components/Skeleton";
import { formatDate, formatMoney } from "../utils";

function PurchaseHistoryPage() {
  const [payments, setPayments] = useState([]);
  const [subscription, setSubscription] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);

  useEffect(function () {
    async function loadData() {
      try {
        const results = await Promise.all([api.get("/account/payments"), api.get("/account/subscription")]);
        setPayments(results[0].data);
        setSubscription(results[1].data);
      } catch (err) {
        setError(true);
      }
      setLoading(false);
    }
    loadData();
  }, []);

  return (
    <div>
      <PageHeader title="Purchase history" subtitle="Your subscription and every payment in one place." />

      <div className="container page">
        {loading && <SkeletonRows rows={5} />}

        {!loading && error && (
          <EmptyState icon={AlertCircle} title="Could not load your purchases" text="Please try again in a moment." />
        )}

        {!loading && !error && (
          <>
            {subscription.isActive ? (
              <div className="status-card status-active">
                <Unlock size={22} />
                <div>
                  <strong>{subscription.planName} subscription · active</strong>
                  <p>
                    Valid until {formatDate(subscription.endDate)} ({subscription.daysLeft} days left)
                  </p>
                </div>
              </div>
            ) : (
              <div className="status-card">
                <Lock size={22} />
                <div>
                  <strong>No active subscription</strong>
                  <p>
                    <Link to="/plans">See subscription plans</Link>
                  </p>
                </div>
              </div>
            )}

            {payments.length === 0 ? (
              <EmptyState icon={Receipt} title="No purchases yet" text="Courses and plans you buy will appear here.">
                <Link to="/courses" className="btn btn-primary">
                  Browse courses
                </Link>
              </EmptyState>
            ) : (
              <div className="card table-card">
                <div className="table-wrapper">
                  <table className="table">
                    <thead>
                      <tr>
                        <th>Date</th>
                        <th>Item</th>
                        <th>Type</th>
                        <th>Amount</th>
                        <th>Status</th>
                        <th>Transaction</th>
                      </tr>
                    </thead>
                    <tbody>
                      {payments.map(function (payment) {
                        return (
                          <tr key={payment.id}>
                            <td>{formatDate(payment.createdAt)}</td>
                            <td>
                              <strong>{payment.itemName}</strong>
                            </td>
                            <td>
                              <span className={payment.paymentType === "Course" ? "badge" : "badge badge-brand"}>
                                {payment.paymentType}
                              </span>
                            </td>
                            <td>{formatMoney(payment.amount)}</td>
                            <td>
                              <span className="badge badge-success">{payment.status}</span>
                            </td>
                            <td className="muted small mono">{payment.transactionId}</td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              </div>
            )}
          </>
        )}
      </div>
    </div>
  );
}

export default PurchaseHistoryPage;
