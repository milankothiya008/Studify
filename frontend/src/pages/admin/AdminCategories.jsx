import { useEffect, useState } from "react";
import api, { getErrorMessage } from "../../api";

function AdminCategories() {
  const [categories, setCategories] = useState([]);
  const [newName, setNewName] = useState("");
  const [error, setError] = useState("");

  function loadCategories() {
    api.get("/categories").then(function (response) {
      setCategories(response.data);
    });
  }

  useEffect(function () {
    loadCategories();
  }, []);

  async function handleAdd(event) {
    event.preventDefault();
    setError("");
    try {
      await api.post("/categories", { name: newName });
      setNewName("");
      loadCategories();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  }

  async function handleRename(category) {
    const name = window.prompt("New name for the category:", category.name);
    if (!name) {
      return;
    }
    try {
      await api.put("/categories/" + category.id, { name: name });
      loadCategories();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  }

  async function handleDelete(category) {
    if (!window.confirm('Delete category "' + category.name + '"? Its courses will have no category.')) {
      return;
    }
    try {
      await api.delete("/categories/" + category.id);
      loadCategories();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  }

  return (
    <div>
      {error && <div className="alert alert-error">{error}</div>}

      <form className="inline-form" onSubmit={handleAdd}>
        <input placeholder="New category name" value={newName} onChange={(e) => setNewName(e.target.value)} required />
        <button type="submit" className="btn btn-primary">
          Add category
        </button>
      </form>

      <div className="table-wrapper">
        <table className="table">
          <thead>
            <tr>
              <th>Name</th>
              <th>Published courses</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {categories.map(function (category) {
              return (
                <tr key={category.id}>
                  <td>{category.name}</td>
                  <td>{category.courseCount}</td>
                  <td>
                    <button className="link-button" onClick={() => handleRename(category)}>
                      Rename
                    </button>{" "}
                    ·{" "}
                    <button className="link-button danger" onClick={() => handleDelete(category)}>
                      Delete
                    </button>
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

export default AdminCategories;
