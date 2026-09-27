import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import api from "../api";
import Spinner from "../components/Spinner";
import { formatDate, formatMoney } from "../utils";

function PurchaseHistoryPage() {
  const [payments, setPayments] = useState([]);
  const [subscription, setSubscription] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(function () {
    async function loadData() {
      const paymentsResponse = await api.get("/account/payments");
      const subscriptionResponse = await api.get("/account/subscription");
      setPayments(paymentsResponse.data);
      setSubscription(subscriptionResponse.data);
      setLoading(false);
    }
    loadData();
  }, []);

  if (loading) {
    return <Spinner />;
  }

  return (
    <div className="container page">
      <h1>Purchase history</h1>

      <div className="box">
        <h3>Subscription</h3>
        {subscription.isActive ? (
          <p>
            <span className="badge badge-success">Active</span> {subscription.planName} plan, valid until{" "}
            <strong>{formatDate(subscription.endDate)}</strong> ({subscription.daysLeft} days left)
          </p>
        ) : (
          <p>
            <span className="badge">Not active</span> <Link to="/plans">See subscription plans</Link>
          </p>
        )}
      </div>

      {payments.length === 0 ? (
        <p className="muted">You haven't bought anything yet.</p>
      ) : (
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
                    <td>{payment.itemName}</td>
                    <td>{payment.paymentType}</td>
                    <td>{formatMoney(payment.amount)}</td>
                    <td>
                      <span className="badge badge-success">{payment.status}</span>
                    </td>
                    <td className="muted small">{payment.transactionId}</td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

export default PurchaseHistoryPage;
