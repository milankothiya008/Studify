import { useEffect, useState } from "react";
import { Search } from "lucide-react";
import api, { getErrorMessage } from "../../api";
import { useAuth } from "../../AuthContext";
import { useToast } from "../../ToastContext";
import Avatar from "../../components/Avatar";
import { SkeletonRows } from "../../components/Skeleton";
import { formatDate } from "../../utils";

function AdminUsers() {
  const { user } = useAuth();
  const showToast = useToast();
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [filterText, setFilterText] = useState("");

  function loadUsers() {
    api
      .get("/admin/users")
      .then(function (response) {
        setUsers(response.data);
      })
      .finally(function () {
        setLoading(false);
      });
  }

  useEffect(function () {
    loadUsers();
  }, []);

  async function changeRole(userId, role) {
    try {
      const response = await api.put("/admin/users/" + userId + "/role", { role: role });
      showToast(response.data.message);
      loadUsers();
    } catch (err) {
      showToast(getErrorMessage(err), "error");
    }
  }

  const text = filterText.trim().toLowerCase();
  const shownUsers = users.filter(function (item) {
    return item.fullName.toLowerCase().includes(text) || item.email.toLowerCase().includes(text);
  });

  if (loading) {
    return <SkeletonRows rows={5} />;
  }

  return (
    <div>
      <div className="table-toolbar">
        <div className="input-icon">
          <Search size={18} />
          <input placeholder="Search by name or email" value={filterText} onChange={(e) => setFilterText(e.target.value)} />
        </div>
        <span className="muted small">{shownUsers.length} users</span>
      </div>

      <div className="card table-card">
        <div className="table-wrapper">
          <table className="table">
            <thead>
              <tr>
                <th>User</th>
                <th>Email</th>
                <th>Joined</th>
                <th>Role</th>
              </tr>
            </thead>
            <tbody>
              {shownUsers.map(function (item) {
                return (
                  <tr key={item.id}>
                    <td>
                      <div className="table-user">
                        <Avatar name={item.fullName} size={34} />
                        <strong>{item.fullName}</strong>
                      </div>
                    </td>
                    <td>
                      {item.email}{" "}
                      {item.isEmailVerified ? (
                        <span className="badge badge-success">Verified</span>
                      ) : (
                        <span className="badge badge-warning">Not verified</span>
                      )}
                    </td>
                    <td>{formatDate(item.createdAt)}</td>
                    <td>
                      <select
                        className="select-small"
                        value={item.role}
                        disabled={item.id === user.id}
                        onChange={(e) => changeRole(item.id, e.target.value)}
                      >
                        <option value="Student">Student</option>
                        <option value="Instructor">Instructor</option>
                        <option value="Admin">Admin</option>
                      </select>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}

export default AdminUsers;
