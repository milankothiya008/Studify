import { useEffect, useState } from "react";
import api, { getErrorMessage } from "../../api";
import { useAuth } from "../../AuthContext";
import { formatDate } from "../../utils";

function AdminUsers() {
  const { user } = useAuth();
  const [users, setUsers] = useState([]);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");

  function loadUsers() {
    api.get("/admin/users").then(function (response) {
      setUsers(response.data);
    });
  }

  useEffect(function () {
    loadUsers();
  }, []);

  async function changeRole(userId, role) {
    setMessage("");
    setError("");
    try {
      const response = await api.put("/admin/users/" + userId + "/role", { role: role });
      setMessage(response.data.message);
      loadUsers();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  }

  return (
    <div>
      {error && <div className="alert alert-error">{error}</div>}
      {message && <div className="alert alert-success">{message}</div>}

      <div className="table-wrapper">
        <table className="table">
          <thead>
            <tr>
              <th>Name</th>
              <th>Email</th>
              <th>Joined</th>
              <th>Role</th>
            </tr>
          </thead>
          <tbody>
            {users.map(function (item) {
              return (
                <tr key={item.id}>
                  <td>{item.fullName}</td>
                  <td>{item.email}</td>
                  <td>{formatDate(item.createdAt)}</td>
                  <td>
                    <select
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
  );
}

export default AdminUsers;
