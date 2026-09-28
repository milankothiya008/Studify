import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Award, MessageCircleQuestion, Table2, BarChart3 } from "lucide-react";
import api from "../../api";
import ColumnChart from "../../components/ColumnChart";
import { SkeletonBlock } from "../../components/Skeleton";
import { formatMoney } from "../../utils";

// Instructor dashboard: enrollments and revenue per day, with a period picker.
// Two separate charts (never one chart with two different scales).
function AnalyticsPanel() {
  const [days, setDays] = useState(30);
  const [analytics, setAnalytics] = useState(null);
  const [showTable, setShowTable] = useState(false);

  useEffect(
    function () {
      setAnalytics(null);
      // getTimezoneOffset() is -330 in India, the API wants +330.
      const utcOffsetMinutes = -new Date().getTimezoneOffset();
      api.get("/instructor/analytics", { params: { days: days, utcOffsetMinutes: utcOffsetMinutes } }).then(function (response) {
        setAnalytics(response.data);
      });
    },
    [days]
  );

  function formatCount(value) {
    return String(Math.round(value));
  }

  return (
    <section className="analytics">
      <div className="section-heading">
        <div>
          <h2>Performance</h2>
          <p>How your courses are doing.</p>
        </div>
        <div className="analytics-controls">
          <select className="select-small" value={days} onChange={(e) => setDays(Number(e.target.value))} aria-label="Period">
            <option value={7}>Last 7 days</option>
            <option value={30}>Last 30 days</option>
            <option value={90}>Last 90 days</option>
          </select>
          <button className="btn btn-outline btn-small" onClick={() => setShowTable(!showTable)}>
            {showTable ? <BarChart3 size={16} /> : <Table2 size={16} />} {showTable ? "Show charts" : "Show table"}
          </button>
        </div>
      </div>

      {!analytics && (
        <div className="chart-grid">
          <SkeletonBlock height={280} />
          <SkeletonBlock height={280} />
        </div>
      )}

      {analytics && (
        <>
          <div className="mini-stats">
            <Link to="/instructor/questions" className={analytics.unansweredQuestions > 0 ? "mini-stat attention" : "mini-stat"}>
              <MessageCircleQuestion size={20} />
              <span>
                <strong>{analytics.unansweredQuestions}</strong>{" "}
                {analytics.unansweredQuestions === 1 ? "question" : "questions"} waiting for your answer
              </span>
            </Link>
            <div className="mini-stat">
              <Award size={20} />
              <span>
                <strong>{analytics.completedStudents}</strong>{" "}
                {analytics.completedStudents === 1 ? "student" : "students"} completed a course
              </span>
            </div>
          </div>

          {showTable ? (
            <div className="card table-card">
              <div className="table-wrapper analytics-table">
                <table className="table">
                  <thead>
                    <tr>
                      <th>Date</th>
                      <th>New students</th>
                      <th>Revenue</th>
                    </tr>
                  </thead>
                  <tbody>
                    {analytics.daily
                      .slice()
                      .reverse()
                      .map(function (day) {
                        return (
                          <tr key={day.date}>
                            <td>{day.date}</td>
                            <td>{day.enrollments}</td>
                            <td>{formatMoney(day.revenue)}</td>
                          </tr>
                        );
                      })}
                  </tbody>
                </table>
              </div>
            </div>
          ) : (
            <div className="chart-grid">
              <div className="card chart-card">
                <ColumnChart
                  title="New students"
                  data={analytics.daily.map((d) => ({ label: d.date, value: d.enrollments }))}
                  formatValue={formatCount}
                  wholeNumbers
                />
              </div>
              <div className="card chart-card">
                <ColumnChart
                  title="Revenue"
                  data={analytics.daily.map((d) => ({ label: d.date, value: Number(d.revenue) }))}
                  formatValue={formatMoney}
                />
              </div>
            </div>
          )}
        </>
      )}
    </section>
  );
}

export default AnalyticsPanel;
